using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Sockets;
using System.Threading;

namespace JamesFrowen.SimpleWeb
{
    public class WebSocketServer
    {
        public readonly ConcurrentQueue<Message> receiveQueue = new ConcurrentQueue<Message>();

        readonly TcpConfig tcpConfig;
        readonly int maxMessageSize;

        TcpListener listener;
        Thread acceptThread;
        bool serverStopped;
        readonly ServerHandshake handShake;
        readonly ServerSslHelper sslHelper;
        readonly BufferPool bufferPool;
        readonly ConcurrentDictionary<int, Connection> connections = new ConcurrentDictionary<int, Connection>();


        int _idCounter = 0;

        public WebSocketServer(TcpConfig tcpConfig, int maxMessageSize, int handshakeMaxSize, SslConfig sslConfig, BufferPool bufferPool)
        {
            this.tcpConfig = tcpConfig;
            this.maxMessageSize = maxMessageSize;
            sslHelper = new ServerSslHelper(sslConfig);
            this.bufferPool = bufferPool;
            handShake = new ServerHandshake(this.bufferPool, handshakeMaxSize);
        }

        public void Listen(int port)
        {
            listener = TcpListener.Create(port);
            listener.Start();

            Log.Info($"Server has started on port {port}");

            acceptThread = new Thread(acceptLoop);
            acceptThread.IsBackground = true;
            acceptThread.Start();
        }

        public void Stop()
        {
            serverStopped = true;

            // Interrupt then stop so that Exception is handled correctly
            acceptThread?.Interrupt();
            listener?.Stop();
            acceptThread = null;


            Log.Info("Server stoped, Closing all connections...");
            // make copy so that foreach doesn't break if values are removed
            Connection[] connectionsCopy = connections.Values.ToArray();
            foreach (Connection conn in connectionsCopy)
            {
                conn.Dispose();
            }

            connections.Clear();
        }

        void acceptLoop()
        {
            try
            {
                try
                {
                    while (true)
                    {
                        TcpClient client = listener.AcceptTcpClient();
                        tcpConfig.ApplyTo(client);


                        // TODO keep track of connections before they are in connections dictionary
                        //      this might not be a problem as HandshakeAndReceiveLoop checks for stop
                        //      and returns/disposes before sending message to queue
                        var conn = new Connection(client, AfterConnectionDisposed);
                        Log.Info($"A client connected {conn}");

                        // handshake needs its own thread as it needs to wait for message from client
                        var receiveThread = new Thread(() => HandshakeAndReceiveLoop(conn));

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
            catch (ThreadInterruptedException e) { Log.InfoException(e); }
            catch (ThreadAbortException e) { Log.InfoException(e); }
            catch (Exception e) { Log.Exception(e); }
        }

        void HandshakeAndReceiveLoop(Connection conn)
        {
            try
            {
                bool success = sslHelper.TryCreateStream(conn);
                if (!success)
                {
                    Log.Error($"Failed to create SSL Stream {conn}");
                    conn.Dispose();
                    return;
                }

                success = handShake.TryHandshake(conn);

                if (success)
                {
                    Log.Info($"Sent Handshake {conn}");
                }
                else
                {
                    Log.Error($"Handshake Failed {conn}");
                    conn.Dispose();
                    return;
                }

                // check if Stop has been called since accepting this client
                if (serverStopped)
                {
                    Log.Info("Server stops after successful handshake");
                    return;
                }

                conn.connId = Interlocked.Increment(ref _idCounter);
                connections.TryAdd(conn.connId, conn);

                receiveQueue.Enqueue(new Message(conn.connId, EventType.Connected));

                var sendThread = new Thread(() =>
                {
                    var sendConfig = new SendLoop.Config(
                        conn,
                        bufferSize: Constants.HeaderSize + maxMessageSize,
                        setMask: false);

                    SendLoop.Loop(sendConfig);
                });

                conn.sendThread = sendThread;
                sendThread.IsBackground = true;
                sendThread.Name = $"SendLoop {conn.connId}";
                sendThread.Start();

                var receiveConfig = new ReceiveLoop.Config(
                    conn,
                    maxMessageSize,
                    expectMask: true,
                    receiveQueue,
                    bufferPool);

                ReceiveLoop.Loop(receiveConfig);
            }
            catch (ThreadInterruptedException e) { Log.InfoException(e); }
            catch (ThreadAbortException e) { Log.InfoException(e); }
            catch (Exception e) { Log.Exception(e); }
            finally
            {
                // close here incase connect fails
                conn.Dispose();
            }
        }

        void AfterConnectionDisposed(Connection conn)
        {
            if (conn.connId != Connection.IdNotSet)
            {
                receiveQueue.Enqueue(new Message(conn.connId, EventType.Disconnected));
                connections.TryRemove(conn.connId, out Connection _);
            }
        }

        public void Send(int id, ArrayBuffer buffer)
        {
            if (connections.TryGetValue(id, out Connection conn))
            {
                conn.sendQueue.Enqueue(buffer);
                conn.sendPending.Set();
            }
            else
            {
                Log.Warn($"Cant send message to {id} because connection was not found in dictionary. Maybe it disconnected.");
            }
        }

        /// <summary>
        /// Sends a large message on main thread, this is blocking till message is sent
        /// </summary>
        /// <param name="id"></param>
        /// <param name="segment"></param>
        public void SendLargeMessage(int id, ArraySegment<byte> segment)
        {
            if (!connections.TryGetValue(id, out Connection conn))
            {
                Log.Warn($"Cant send message to {id} because connection was not found in dictionary. Maybe it disconnected.");
                return;
            }

            if (!conn.AllowLargeMessage)
                throw new InvalidOperationException("Large message is disabled set AllowLargeMessage to true first");

            // wait for send queue to be empty
            while (conn.sendQueue.Count > 0)
            {
                Thread.Sleep(1);
            }

            NetworkStream stream = conn.client.GetStream();
            // write header
            // 14 is max header size
            byte[] header = new byte[10];
            int length = SendLoop.WriteHeader(header, 0, segment.Count, false);

            // write large message
            stream.Write(header, 0, length);
            stream.Write(segment.Array, segment.Offset, segment.Count);
        }

        public bool CloseConnection(int id)
        {
            if (connections.TryGetValue(id, out Connection conn))
            {
                Log.Info($"Kicking connection {id}");
                conn.Dispose();
                return true;
            }
            else
            {
                Log.Warn($"Failed to kick {id} because id not found");

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
                Log.Error($"Cant get address of connection {id} because connection was not found in dictionary");
                return null;
            }
        }

        /// <summary>
        /// Allows large messages from connection
        /// <para>WARNING: large message will cause buffers to be allocated which may cause negative performance</para>
        /// </summary>
        /// <param name="connectionId"></param>
        /// <param name="enabled"></param>
        public void AllowLargeMessage(int id, bool enabled)
        {
            if (connections.TryGetValue(id, out Connection conn))
            {
                conn.AllowLargeMessage = enabled;
            }
            else
            {
                Log.Error($"Cant set AllowLargeMessage for connection {id} because connection was not found in dictionary");
            }
        }

        /// <summary>
        /// Allows large messages from connection
        /// <para>WARNING: large message will cause buffers to be allocated which may cause negative performance</para>
        /// </summary>
        /// <param name="connectionId"></param>
        /// <param name="enabled"></param>
        public bool IsLargeMessageAllowed(int id)
        {
            if (connections.TryGetValue(id, out Connection conn))
            {
                return conn.AllowLargeMessage;
            }
            else
            {
                Log.Error($"Cant get IsLargeMessageAllowed for connection {id} because connection was not found in dictionary");
                return false;
            }
        }
    }
}
