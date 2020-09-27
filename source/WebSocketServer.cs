#define SIMPLE_WEB_INFO_LOG
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
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
        readonly Handshake handShake = new Handshake();
        readonly SslHelper sslHelper;
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
            this.sslConfig = sslConfig ?? new SslConfig();
            sslHelper = new SslHelper(this.sslConfig);
        }

        public void Listen(short port)
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


                        Connection conn = new Connection
                        {
                            client = client,
                        };
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
                    CheckForInterupt();
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
                Log.Info($"Failed to create SSL Stream {conn}");
                conn.client.Dispose();
                return;
            }

            success = handShake.TryHandshake(conn);

            if (!success)
            {
                Log.Info($"Handshake Failed {conn}");
                conn.client.Dispose();
                return;
            }

            conn.connId = GetNextId();
            connections.TryAdd(conn.connId, conn);

            receiveQueue.Enqueue(new Message
            {
                connId = conn.connId,
                type = EventType.Connected
            });

            Thread sendThread = new Thread(() => SendLoop(conn));

            conn.sendThread = sendThread;
            sendThread.IsBackground = true;
            sendThread.Start();

            ReceiveLoop(conn);
        }


        void ReceiveLoop(Connection conn)
        {
            try
            {
                TcpClient client = conn.client;
                Stream stream = conn.stream;
                //byte[] buffer = conn.receiveBuffer;
                const int HeaderLength = 4;
                byte[] headerBuffer = new byte[HeaderLength];

                while (client.Connected)
                {
                    // header is at most 4 bytes + mask
                    // 1 for bit fields
                    // 1+ for length (length can be be 1, 3, or 9 and we refuse 9)
                    // 4 for mask (we can read this later
                    bool success = ReadHelper.SafeRead(stream, headerBuffer, 0, HeaderLength);
                    if (!success)
                    {
                        Log.Info($"ReceiveLoop {conn.connId} not connected or timed out");
                        CheckForInterupt();
                        // will go to finally block below
                        break;
                    }

                    MessageProcessor.Result result = MessageProcessor.ProcessHeader(headerBuffer, maxMessageSize);

                    // todo remove allocation
                    // mask + msg
                    byte[] buffer = new byte[HeaderLength + result.readLength];
                    for (int i = 0; i < HeaderLength; i++)
                    {
                        // copy header as it might contain mask
                        buffer[i] = headerBuffer[i];
                    }

                    ReadHelper.SafeRead(stream, buffer, HeaderLength, result.readLength);

                    MessageProcessor.DecodeMessage(buffer, result.maskOffset, result.msgLength);

                    HandleMessage(result.opcode, conn, buffer, result.msgOffset, result.msgLength);
                }
            }
            catch (ObjectDisposedException) { Log.Info($"ReceiveLoop {conn} Stream closed"); return; }
            catch (ThreadInterruptedException) { Log.Info($"ReceiveLoop {conn} ThreadInterrupted"); return; }
            catch (ThreadAbortException) { Log.Info($"ReceiveLoop {conn} ThreadAbort"); return; }
            catch (InvalidDataException e)
            {
                receiveQueue.Enqueue(new Message
                {
                    connId = conn.connId,
                    type = EventType.Error,
                    exception = e
                });
            }
            catch (Exception e) { Debug.LogException(e); }
            finally
            {
                CloseConnection(conn);
            }
        }
        static void CheckForInterupt()
        {
            // sleep in order to check for ThreadInterruptedException
            Thread.Sleep(1);
        }

        void HandleMessage(int opcode, Connection conn, byte[] buffer, int offset, int length)
        {
            if (opcode == 2)
            {
                ArraySegment<byte> data = new ArraySegment<byte>(buffer, offset, length);

                receiveQueue.Enqueue(new Message
                {
                    connId = conn.connId,
                    type = EventType.Data,
                    data = data,
                });
            }
            else if (opcode == 8)
            {
                Log.Info($"Close: {buffer[offset + 0] << 8 | buffer[offset + 1]} message:{Encoding.UTF8.GetString(buffer, offset + 2, length - 2)}");
                CloseConnection(conn);
            }
        }


        void SendLoop(Connection conn)
        {
            try
            {
                TcpClient client = conn.client;
                Stream stream = conn.stream;
                while (client.Connected)
                {
                    // wait for message
                    conn.sendPending.WaitOne();
                    conn.sendPending.Reset();

                    while (conn.sendQueue.TryDequeue(out ArraySegment<byte> msg))
                    {
                        // check if connected before sending message
                        if (!client.Connected) { Log.Info($"SendLoop {conn} not connected"); return; }

                        SendMessageToClient(stream, msg);
                    }
                }
            }
            catch (ThreadInterruptedException) { Log.Info($"SendLoop {conn} ThreadInterrupted"); return; }
            catch (ThreadAbortException) { Log.Info($"SendLoop {conn} ThreadAbort"); return; }
            catch (Exception e)
            {
                Debug.LogException(e);

                CloseConnection(conn);
            }
        }

        void CloseConnection(Connection conn)
        {
            bool closed = conn.Close();
            // only send disconnect message if closed by the call
            if (closed)
            {
                receiveQueue.Enqueue(new Message { connId = conn.connId, type = EventType.Disconnected });
                connections.TryRemove(conn.connId, out Connection _);
            }
        }

        static void SendMessageToClient(Stream stream, ArraySegment<byte> msg)
        {
            int msgLength = msg.Count;
            // todo remove allocation
            byte[] buffer = new byte[4 + msgLength];
            int sendLength = 0;

            byte finished = 128;
            byte byteOpCode = 2;

            buffer[0] = (byte)(finished | byteOpCode);
            sendLength++;

            if (msgLength < 125)
            {
                buffer[1] = (byte)msgLength;
                sendLength++;
            }
            else if (msgLength < ushort.MaxValue)
            {
                buffer[1] = 126;
                buffer[2] = (byte)(msgLength >> 8);
                buffer[3] = (byte)msgLength;
                sendLength += 3;
            }
            else
            {
                throw new InvalidDataException($"Trying to send a message larger than {ushort.MaxValue} bytes");
            }

            Array.Copy(msg.Array, msg.Offset, buffer, sendLength, msgLength);
            sendLength += msgLength;

            stream.Write(buffer, 0, sendLength);

            Debug.Log("Sent message to client");
        }

        public void Send(int id, ArraySegment<byte> segment)
        {
            if (connections.TryGetValue(id, out Connection conn))
            {
                // todo remove allocation
                byte[] buffer = new byte[segment.Count];

                Array.Copy(segment.Array, segment.Offset, buffer, 0, segment.Count);

                conn.sendQueue.Enqueue(new ArraySegment<byte>(buffer));
                conn.sendPending.Set();
            }
            else
            {
                Debug.LogError($"Cant send message to {id} because connection was not found in dictionary");
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
