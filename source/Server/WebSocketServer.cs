using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using Debug = UnityEngine.Debug;

namespace Mirror.SimpleWeb
{
    public class WebSocketServer
    {
        public readonly ConcurrentQueue<Message> receiveQueue = new ConcurrentQueue<Message>();

        readonly bool noDelay;
        readonly int sendTimeout;
        readonly int receiveTimeout;
        readonly int maxMessageSize;
        readonly SslConfig sslConfig;

        TcpListener listener;
        Thread acceptThread;
        readonly ServerHandshake handShake = new ServerHandshake();
        readonly ServerSslHelper sslHelper;
        readonly ConcurrentDictionary<int, Connection> connections = new ConcurrentDictionary<int, Connection>();

        int _previousId = 0;

        int GetNextId()
        {
            _previousId++;
            return _previousId;
        }

        public WebSocketServer(bool noDelay, int sendTimeout, int receiveTimeout, int maxMessageSize, SslConfig sslConfig)
        {
            this.noDelay = noDelay;
            this.sendTimeout = sendTimeout;
            this.receiveTimeout = receiveTimeout;
            this.maxMessageSize = maxMessageSize;
            this.sslConfig = sslConfig;
            sslHelper = new ServerSslHelper(this.sslConfig);
        }

        public void Listen(int port)
        {
            listener = TcpListener.Create(port);
            listener.Server.NoDelay = noDelay;
            listener.Server.SendTimeout = sendTimeout;
            listener.Start();

            Debug.Log($"Server has started on port {port}.\nWaiting for a connection...");

            acceptThread = new Thread(acceptLoop);
            acceptThread.IsBackground = true;
            acceptThread.Start();
        }

        public void Stop()
        {
            // Interrupt then stop so that Exception is handled correctly
            acceptThread?.Interrupt();
            listener?.Stop();
            acceptThread = null;

            Connection[] connections = this.connections.Values.ToArray();
            foreach (Connection conn in connections)
            {
                conn.Close();
            }

            this.connections.Clear();
        }

        void acceptLoop()
        {
            try
            {
                try
                {
                    while (true)
                    {
                        // TODO check this is blocking?
                        TcpClient client = listener.AcceptTcpClient();
                        client.SendTimeout = sendTimeout;
                        client.ReceiveTimeout = receiveTimeout;
                        client.NoDelay = noDelay;

                        Connection conn = new Connection(client);
                        Log.Info($"A client connected {conn}");

                        // handshake needs its own thread as it needs to wait for message from client
                        Thread receiveThread = new Thread(() => HandshakeAndReceiveLoop(conn));

                        conn.receiveThread = receiveThread;

                        receiveThread.IsBackground = true;
                        receiveThread.Start();
                    }
                }
                catch (SocketException)
                {
                    // check for Interrupted/Abort
                    Utils.CheckForInterupt();
                    throw;
                }
            }
            catch (ThreadInterruptedException) { Log.Info("acceptLoop ThreadInterrupted"); return; }
            catch (ThreadAbortException) { Log.Info("acceptLoop ThreadAbort"); return; }
            catch (Exception e) { Debug.LogException(e); }
        }

        void HandshakeAndReceiveLoop(Connection conn)
        {
            bool success = sslHelper.TryCreateStream(conn);
            if (!success)
            {
                Log.Error($"Failed to create SSL Stream {conn}");
                conn.client.Dispose();
                return;
            }

            success = handShake.TryHandshake(conn);

            if (!success)
            {
                Log.Error($"Handshake Failed {conn}");
                conn.client.Dispose();
                return;
            }

            conn.connId = GetNextId();
            connections.TryAdd(conn.connId, conn);

            receiveQueue.Enqueue(new Message(conn.connId, EventType.Connected));

            Thread sendThread = new Thread(() =>
            {
                int bufferSize = Constants.HeaderSize + maxMessageSize;
                SendLoop.Loop(conn, bufferSize, false, CloseConnection);
            });

            conn.sendThread = sendThread;
            sendThread.IsBackground = true;
            sendThread.Start();

            ReceiveLoop.Loop(conn, maxMessageSize, true, receiveQueue, CloseConnection);
        }

        void CloseConnection(Connection conn)
        {
            bool closed = conn.Close();
            // only send disconnect message if closed by the call
            if (closed)
            {
                receiveQueue.Enqueue(new Message(conn.connId, EventType.Disconnected));
                connections.TryRemove(conn.connId, out Connection _);
            }
        }

        public void Send(int id, ArraySegment<byte> segment)
        {
            if (connections.TryGetValue(id, out Connection conn))
            {
                conn.sendQueue.Enqueue(segment);
                conn.sendPending.Set();
            }
            else
            {
                Log.Warn($"Cant send message to {id} because connection was not found in dictionary. Maybe it disconnected.");
            }
        }

        public bool CloseConnection(int id)
        {
            if (connections.TryGetValue(id, out Connection conn))
            {
                CloseConnection(conn);
                return true;
            }
            else
            {
                return false;
            }
        }

        public string GetClientAddress(int id)
        {
            if (connections.TryGetValue(id, out Connection conn))
            {
                return conn.client.Client.RemoteEndPoint.ToString();
            }
            else
            {
                Debug.LogError($"Cant close connection to {id} because connection was not found in dictionary");
                return null;
            }
        }
    }
}
