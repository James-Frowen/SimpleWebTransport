using System;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;

namespace Mirror.SimpleWeb
{
    public class WebSocketClientStandAlone : SimpleWebClient
    {
        readonly object lockObject = new object();
        bool hasClosed;

        readonly ClientSslHelper sslHelper;
        readonly ClientHandshake handshake;
        readonly RNGCryptoServiceProvider random;

        private Connection conn;
        readonly int sendTimeout;
        readonly int receiveTimeout;

        internal WebSocketClientStandAlone(int maxMessageSize, int maxMessagesPerTick, int sendTimeout, int receiveTimeout) : base(maxMessageSize, maxMessagesPerTick)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            throw new NotSupportedException();
#else
            sslHelper = new ClientSslHelper();
            handshake = new ClientHandshake();
            random = new RNGCryptoServiceProvider();
            this.sendTimeout = sendTimeout;
            this.receiveTimeout = receiveTimeout;
#endif
        }
        ~WebSocketClientStandAlone()
        {
            random?.Dispose();
        }

        public override void Connect(string address)
        {
            state = ClientState.Connecting;
            Thread receiveThread = new Thread(() => ConnectAndReceiveLoop(address));
            receiveThread.IsBackground = true;
            receiveThread.Start();
        }

        void ConnectAndReceiveLoop(string address)
        {
            try
            {
                TcpClient client = new TcpClient();
                client.NoDelay = true;
                client.ReceiveTimeout = receiveTimeout;
                client.SendTimeout = sendTimeout;
                Uri uri = new Uri(address);
                try
                {
                    client.Connect(uri.Host, uri.Port);
                }
                catch (SocketException)
                {
                    client.Dispose();
                    throw;
                }

                conn = new Connection(client);
                conn.receiveThread = Thread.CurrentThread;

                bool success = sslHelper.TryCreateStream(conn, uri);
                if (!success)
                {
                    conn.Close();
                    return;
                }

                success = handshake.TryHandshake(conn, uri);
                if (!success)
                {
                    conn.Close();
                    return;
                }

                Log.Info("HandShake Successful");

                state = ClientState.Connected;

                receiveQueue.Enqueue(new Message(EventType.Connected));

                Thread sendThread = new Thread(() =>
                {
                    int bufferSize = Constants.HeaderSize + Constants.MaskSize + maxMessageSize;
                    SendLoop.Loop(conn, bufferSize, true, _ => CloseConnection());
                });

                conn.sendThread = sendThread;
                sendThread.IsBackground = true;
                sendThread.Start();

                ReceiveLoop.Loop(conn, maxMessageSize, false, receiveQueue, _ => CloseConnection(), bufferPool);
            }
            catch (ThreadInterruptedException) { Log.Info("acceptLoop ThreadInterrupted"); }
            catch (ThreadAbortException) { Log.Info("acceptLoop ThreadAbort"); }
            catch (Exception e) { Log.Exception(e); }
            finally
            {
                // close here incase connect fails
                CloseConnection();
            }
        }

        void CloseConnection()
        {
            conn?.Close();

            if (hasClosed) { return; }

            // lock so that hasClosed can be safely set
            lock (lockObject)
            {
                hasClosed = true;

                state = ClientState.NotConnected;
                // make sure Disconnected event is only called once
                receiveQueue.Enqueue(new Message(EventType.Disconnected));
            }
        }

        public override void Disconnect()
        {
            CloseConnection();
        }

        public override void Send(ArraySegment<byte> segment)
        {
            ArrayBuffer buffer = bufferPool.Take(segment.Count);
            buffer.CopyFrom(segment);

            conn.sendQueue.Enqueue(buffer);
            conn.sendPending.Set();
        }
    }
}
