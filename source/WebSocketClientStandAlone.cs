using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Mirror.SimpleWeb
{
    internal class WebSocketClientStandAlone : IWebSocketClient
    {
        const int HeaderLength = 4;

        public enum State
        {
            NotConnected = 0,
            Connecting = 1,
            Connected = 2,
            Disconnecting = 3,
        }

        public readonly Queue<Message> receiveQueue = new Queue<Message>();
        readonly ClientSslHelper sslHelper;
        readonly ClientHandshake handshake;
        readonly RNGCryptoServiceProvider random;
        readonly int maxMessageSize;

        private Connection conn;
        private State state;

#if UNITY_WEBGL && !UNITY_EDITOR
        internal WebSocketClientStandAlone(int maxMessageSize) => throw new NotSupportedException();
#else
        internal WebSocketClientStandAlone(int maxMessageSize)
        {
            this.maxMessageSize = maxMessageSize;
            sslHelper = new ClientSslHelper();
            handshake = new ClientHandshake();
        }
#endif

        public bool IsConnected { get; private set; }

        public event Action onConnect;
        public event Action onDisconnect;
        public event Action<ArraySegment<byte>> onData;
        public event Action onError;

        public void Connect(string address)
        {
            state = State.Connecting;
            Thread receiveThread = new Thread(() => ConnectAndReceiveLoop(address));
            receiveThread.IsBackground = true;
            receiveThread.Start();
        }
        void ConnectAndReceiveLoop(string address)
        {
            try
            {
                TcpClient client = new TcpClient();
                Uri uri = new Uri(address);
                client.Connect(uri.Host, uri.Port);

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

                state = State.Connected;

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
            catch (ThreadInterruptedException) { Log.Info("acceptLoop ThreadInterrupted"); return; }
            catch (ThreadAbortException) { Log.Info("acceptLoop ThreadAbort"); return; }
            catch (Exception e) { Debug.LogException(e); }
        }

        void ReceiveLoop(Connection conn)
        {
            // todo remove duplicate code (this and WebSocketServer)
            try
            {
                TcpClient client = conn.client;
                Stream stream = conn.stream;
                //byte[] buffer = conn.receiveBuffer;
                byte[] headerBuffer = new byte[HeaderLength];

                while (client.Connected)
                {
                    bool success = ReadOneMessage(conn, stream, headerBuffer);
                    if (!success)
                        break;
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

        private bool ReadOneMessage(Connection conn, Stream stream, byte[] headerBuffer)
        {
            // header is at most 4 bytes + mask
            // 1 for bit fields
            // 1+ for length (length can be be 1, 3, or 9 and we refuse 9)
            // 4 for mask (we can read this later
            ReadHelper.ReadResult readResult = ReadHelper.SafeRead(stream, headerBuffer, 0, HeaderLength, checkLength: true);
            if ((readResult & ReadHelper.ReadResult.Fail) > 0)
            {
                Log.Info($"ReceiveLoop {conn.connId} read failed: {readResult}");
                CheckForInterupt();
                // will go to finally block below
                return false;
            }

            MessageProcessor.Result header = MessageProcessor.ProcessHeader(headerBuffer, maxMessageSize, false);

            // todo remove allocation
            // msg
            byte[] buffer = new byte[HeaderLength + header.readLength];
            for (int i = 0; i < HeaderLength; i++)
            {
                // copy header as it might contain mask
                buffer[i] = headerBuffer[i];
            }

            ReadHelper.SafeRead(stream, buffer, HeaderLength, header.readLength);

            HandleMessage(header.opcode, conn, buffer, header.msgOffset, header.msgLength);
            return true;
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
            // todo remove duplicate code (this and WebSocketServer)
            try
            {
                TcpClient client = conn.client;
                Stream stream = conn.stream;

                // null check incase disconnect while send thread is starting
                if (client == null)
                    return;

                while (client.Connected)
                {
                    // wait for message
                    conn.sendPending.WaitOne();
                    conn.sendPending.Reset();

                    while (conn.sendQueue.TryDequeue(out ArraySegment<byte> msg))
                    {
                        // check if connected before sending message
                        if (!client.Connected) { Log.Info($"SendLoop {conn} not connected"); return; }

                        SendMessageToServer(stream, msg);
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
            }
        }

        byte[] maskBuffer = new byte[4];
        void SendMessageToServer(Stream stream, ArraySegment<byte> msg)
        {
            int msgLength = msg.Count;
            // todo remove allocation
            // header 2/4 + mask + msg
            byte[] buffer = new byte[4 + 4 + msgLength];
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

            // mask
            buffer[1] |= 0b1000_000;
            random.GetBytes(maskBuffer);
            Array.Copy(maskBuffer, 0, buffer, sendLength, 4);
            sendLength += 4;

            Array.Copy(msg.Array, msg.Offset, buffer, sendLength, msgLength);
            MessageProcessor.ToggleMask(buffer, sendLength, msgLength, buffer, sendLength - 4);
            sendLength += msgLength;

            stream.Write(buffer, 0, sendLength);
        }

        public void Disconnect()
        {
            conn?.Close();
        }

        public void Send(ArraySegment<byte> source)
        {
            byte[] buffer = new byte[source.Count];
            Array.Copy(source.Array, source.Offset, buffer, 0, source.Count);
            ArraySegment<byte> copy = new ArraySegment<byte>(buffer);

            conn.sendQueue.Enqueue(copy);
            conn.sendPending.Set();
        }
    }
}
