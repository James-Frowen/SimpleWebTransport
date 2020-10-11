using System;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;
using UnityEngine;

namespace Mirror.SimpleWeb
{
    internal class WebSocketClientStandAlone : SimpleWebClient
    {
        object lockObject = new object();
        bool hasClosed;

        readonly ClientSslHelper sslHelper;
        readonly ClientHandshake handshake;
        readonly RNGCryptoServiceProvider random;

        private Connection conn;

        internal WebSocketClientStandAlone(int maxMessageSize, int maxMessagesPerTick) : base(maxMessageSize, maxMessagesPerTick)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            throw new NotSupportedException();
#else
            sslHelper = new ClientSslHelper();
            handshake = new ClientHandshake();
            random = new RNGCryptoServiceProvider();
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
                client.ReceiveTimeout = 20000;
                client.SendTimeout = 5000;
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
            catch (ThreadInterruptedException) { Log.Info("acceptLoop ThreadInterrupted"); return; }
            catch (ThreadAbortException) { Log.Info("acceptLoop ThreadAbort"); return; }
            catch (Exception e) { Debug.LogException(e); }
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

        public override void Send(ArraySegment<byte> source)
        {
            ArrayBuffer buffer = bufferPool.Take(source.Count);
            buffer.CopyFrom(source);

            conn.sendQueue.Enqueue(buffer);
            conn.sendPending.Set();
        }
    }
}
