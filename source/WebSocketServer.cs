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

        TcpListener listener;
        Thread acceptThread;
        readonly HandShake handShake = new HandShake();
        readonly ConcurrentDictionary<int, Connection> connections = new ConcurrentDictionary<int, Connection>();

        int _previousId = 0;
        int GetNextId()
        {
            _previousId++;
            return _previousId;
        }

        public WebSocketServer(bool noDelay, int sendTimeout, int receiveTimeout, int maxMessageSize)
        {
            this.noDelay = noDelay;
            this.sendTimeout = sendTimeout;
            this.receiveTimeout = receiveTimeout;
            this.maxMessageSize = maxMessageSize;
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
            listener?.Stop();
            acceptThread?.Interrupt();
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

                        Log.Info("A client connected.");

                        Connection conn = new Connection
                        {
                            client = client,
                        };

                        // handshake needs its own thread as it needs to wait for message from client
                        Thread receiveThread = new Thread(() => HandshakeAndReceiveLoop(conn));

                        conn.receiveThread = receiveThread;

                        receiveThread.IsBackground = true;
                        receiveThread.Start();
                    }
                }
                catch (SocketException e)
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
            bool success = handShake.TryHandshake(conn.client);

            if (!success)
            {
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

            conn.receiveBuffer = new byte[maxMessageSize];

            ReceiveLoop(conn);
        }


        void ReceiveLoop(Connection conn)
        {
            try
            {
                TcpClient client = conn.client;
                NetworkStream stream = client.GetStream();
                byte[] buffer = conn.receiveBuffer;

                while (true)
                {
                    // we expect atleast 6 bytes
                    // 1 for bit fields
                    // 1+ for length (length can be be 1, 2, or 4)
                    // 4 for mask
                    bool success = ReadHelper.SafeRead(stream, buffer, 0, 6);
                    if (!success)
                    {
                        Log.Info($"ReceiveLoop {conn.connId} not connected or timed out");
                        CheckForInterupt();
                        // will go to finally block below
                        break;
                    }


                    int length = client.Available;
                    ReadHelper.SafeRead(stream, buffer, 6, length);

                    ProcessMessages(conn, buffer, length);
                }
            }
            catch (ObjectDisposedException e) { Log.Info($"ReceiveLoop {conn.connId} Stream closed"); return; }
            catch (ThreadInterruptedException) { Log.Info($"ReceiveLoop {conn.connId} ThreadInterrupted"); return; }
            catch (ThreadAbortException) { Log.Info($"ReceiveLoop {conn.connId} ThreadAbort"); return; }
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

        void ProcessMessages(Connection conn, byte[] buffer, int length)
        {
            int bytesProcessed = ProcessMessage(conn, buffer, 0, length);

            while (bytesProcessed < length)
            {
                bytesProcessed = ProcessMessage(conn, buffer, bytesProcessed, length);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="buffer"></param>
        /// <param name="length"></param>
        /// <returns>bytes processed</returns>
        int ProcessMessage(Connection conn, byte[] buffer, int offset, int length)
        {
            bool finished = (buffer[offset + 0] & 0b10000000) != 0; // has full message been sent
            bool hasMask = (buffer[offset + 1] & 0b10000000) != 0; // must be true, "All messages from the client to the server have this bit set"

            int opcode = buffer[offset + 0] & 0b00001111; // expecting 1 - text message
            byte lenByte = (byte)(buffer[offset + 1] - 128); // & 0111 1111

            throwIfNotFinished(finished);
            throwIfNoMask(hasMask);
            throwIfBadOpCode(opcode);

            int msglen;
            (msglen, offset) = GetMessageLength(buffer, offset, lenByte);

            throwIfLengthZero(msglen);
            throwIfMsgLengthTooLong(msglen, length);

            int maskOffset = offset;
            offset += 4;

            for (int i = 0; i < msglen; i++)
            {
                byte maskByte = buffer[maskOffset + i % 4];
                buffer[offset + i] = (byte)(buffer[offset + i] ^ maskByte);
            }

            if (opcode == 2)
            {
                ArraySegment<byte> data = new ArraySegment<byte>(buffer, offset, msglen);

                receiveQueue.Enqueue(new Message
                {
                    connId = conn.connId,
                    type = EventType.Data,
                    data = data,
                });
            }
            else if (opcode == 8)
            {
                Log.Info($"Close: {buffer[offset + 0] << 8 | buffer[offset + 1]} message:{Encoding.UTF8.GetString(buffer, offset + 2, msglen - 2)}");
                CloseConnection(conn);
            }

            return offset + msglen;
        }


        /// <exception cref="InvalidDataException"></exception>
        static void throwIfNotFinished(bool finished)
        {
            if (!finished)
            {
                // TODO check if we need to deal with this
                throw new InvalidDataException("Full message should have been sent, if the full message wasn't sent it wasn't sent from this trasnport");
            }
        }

        /// <exception cref="InvalidDataException"></exception>
        static void throwIfNoMask(bool hasMask)
        {
            if (!hasMask)
            {
                throw new InvalidDataException("Message from client should have mask set to true");
            }
        }

        /// <exception cref="InvalidDataException"></exception>
        static void throwIfBadOpCode(int opcode)
        {
            // 2 = binary
            // 8 = close
            if (opcode != 2 && opcode != 8)
            {
                throw new InvalidDataException("Expected opcode to be binary or close");
            }
        }

        /// <exception cref="InvalidDataException"></exception>
        static void throwIfLengthZero(int msglen)
        {
            if (msglen == 0)
            {
                throw new InvalidDataException("Message length was zero");
            }
        }

        /// <summary>
        /// need to check this so that data from previous buffer isnt used
        /// </summary>
        /// <exception cref="InvalidDataException"></exception>
        static void throwIfMsgLengthTooLong(int msglen, int readLength)
        {
            if (msglen > readLength)
            {
                throw new InvalidDataException("Message length was longer than read length");
            }
        }

        static (int length, int offset) GetMessageLength(byte[] buffer, int offset, byte lenByte)
        {
            if (lenByte == 126)
            {
                ushort value = 0;
                value |= buffer[offset + 2];
                value |= (ushort)(buffer[offset + 3] << 8);

                return (value, offset + 4);
            }
            else if (lenByte == 127)
            {
                throw new Exception("Max length is longer than allowed in a single message");

                //ulong value = 0;
                //value |= buffer[2];
                //value |= (ulong)buffer[3] << 8;
                //value |= (ulong)buffer[4] << 16;
                //value |= (ulong)buffer[5] << 24;
                //value |= (ulong)buffer[6] << 32;
                //value |= (ulong)buffer[7] << 40;
                //value |= (ulong)buffer[8] << 48;
                //value |= (ulong)buffer[9] << 56;

                //return (value, 10);
            }
            else // is less than 126
            {
                return (lenByte, offset + 2);
            }
        }

        void SendLoop(Connection conn)
        {
            try
            {
                TcpClient client = conn.client;
                NetworkStream stream = client.GetStream();
                while (client.Connected)
                {
                    // wait for message
                    conn.sendPending.WaitOne();
                    conn.sendPending.Reset();

                    while (conn.sendQueue.TryDequeue(out ArraySegment<byte> msg))
                    {
                        // check if connected before sending message
                        if (!client.Connected) { Log.Info($"SendLoop {conn.connId} not connected"); return; }

                        SendMessageToClient(stream, msg);
                    }
                }
            }
            catch (ThreadInterruptedException) { Log.Info($"SendLoop {conn.connId} ThreadInterrupted"); return; }
            catch (ThreadAbortException) { Log.Info($"SendLoop {conn.connId} ThreadAbort"); return; }
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

        static void SendMessageToClient(NetworkStream stream, ArraySegment<byte> msg)
        {
            int msgLength = msg.Count;
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
                buffer[2] = (byte)msgLength;
                buffer[3] = (byte)(msgLength >> 8);
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
                conn.sendQueue.Enqueue(segment);
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
                Debug.LogError($"Cant close connection to {id} because connection was not found in dictionary");
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
