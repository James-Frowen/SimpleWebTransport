using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Mirror.SimpleWeb
{
    internal static class ReceiveLoop
    {
        public struct Config
        {
            public readonly Connection conn;
            public readonly int maxMessageSize;
            public readonly bool expectMask;
            public readonly ConcurrentQueue<Message> queue;
            public readonly Action<Connection> closeCallback;
            public readonly BufferPool bufferPool;

            public Config(Connection conn, int maxMessageSize, bool expectMask, ConcurrentQueue<Message> queue, Action<Connection> closeCallback, BufferPool bufferPool)
            {
                this.conn = conn ?? throw new ArgumentNullException(nameof(conn));
                this.maxMessageSize = maxMessageSize;
                this.expectMask = expectMask;
                this.queue = queue ?? throw new ArgumentNullException(nameof(queue));
                this.closeCallback = closeCallback ?? throw new ArgumentNullException(nameof(closeCallback));
                this.bufferPool = bufferPool ?? throw new ArgumentNullException(nameof(bufferPool));
            }

            public void Deconstruct(out Connection conn, out int maxMessageSize, out bool expectMask, out ConcurrentQueue<Message> queue, out Action<Connection> closeCallback, out BufferPool bufferPool)
            {
                conn = this.conn;
                maxMessageSize = this.maxMessageSize;
                expectMask = this.expectMask;
                queue = this.queue;
                closeCallback = this.closeCallback;
                bufferPool = this.bufferPool;
            }
        }

        public static void Loop(Config config)
        {
            (Connection conn, int maxMessageSize, bool expectMask, ConcurrentQueue<Message> queue, Action<Connection> closeCallback, BufferPool _) = config;

            byte[] readBuffer = new byte[Constants.HeaderSize + (expectMask ? Constants.MaskSize : 0) + maxMessageSize];
            try
            {
                try
                {
                    TcpClient client = conn.client;

                    while (client.Connected)
                    {
                        bool success = ReadOneMessage(config, readBuffer);
                        if (!success)
                            break;
                    }
                }
                catch (Exception)
                {
                    // if interupted we dont care about other execptions
                    Utils.CheckForInterupt();
                    throw;
                }
            }
            catch (ThreadInterruptedException) { Log.Info($"ReceiveLoop {conn} ThreadInterrupted"); }
            catch (ThreadAbortException) { Log.Info($"ReceiveLoop {conn} ThreadAbort"); }
            catch (ObjectDisposedException) { Log.Info($"ReceiveLoop {conn} Stream closed"); }
            catch (ReadHelperException e)
            {
                Log.Info($"ReceiveLoop {conn.connId} read failed: {e.Message}");
            }
            catch (SocketException e)
            {
                // this could happen if wss client closes stream
                Log.Warn($"ReceiveLoop SocketException\n{e.Message}", false);
            }
            catch (IOException e)
            {
                // this could happen if client disconnects
                Log.Warn($"ReceiveLoop IOException\n{e.Message}", false);
            }
            catch (InvalidDataException e)
            {
                Log.Error($"Invalid data from {conn}: {e.Message}");
                queue.Enqueue(new Message(conn.connId, e));
            }
            catch (Exception e)
            {
                Log.Exception(e);
                queue.Enqueue(new Message(conn.connId, e));
            }
            finally
            {
                closeCallback.Invoke(conn);
            }
        }

        static bool ReadOneMessage(Config config, byte[] buffer)
        {
            (Connection conn, int maxMessageSize, bool expectMask, ConcurrentQueue<Message> queue, Action<Connection> closeCallback, BufferPool bufferPool) = config;
            Stream stream = conn.stream;

            int offset = 0;
            // read 2
            offset = ReadHelper.Read(stream, buffer, offset, Constants.HeaderMinSize);
            // log after first blocking call
            Log.Verbose($"Message From {conn}");

            if (MessageProcessor.NeedToReadShortLength(buffer))
            {
                offset = ReadHelper.Read(stream, buffer, offset, Constants.ShortLength);
            }


            MessageProcessor.ValidateHeader(buffer, maxMessageSize, expectMask);

            if (expectMask)
            {
                offset = ReadHelper.Read(stream, buffer, offset, Constants.MaskSize);
            }

            int opcode = MessageProcessor.GetOpcode(buffer);
            int payloadLength = MessageProcessor.GetPayloadLength(buffer);

            Log.Verbose($"Header ln:{payloadLength} op:{opcode} mask:{expectMask}");

            offset = ReadHelper.Read(stream, buffer, offset, payloadLength);

            int msgOffset = offset - payloadLength;

            Log.DumpBuffer($"Raw Header", buffer, 0, msgOffset);
            switch (opcode)
            {
                case 2:
                    HandleArrayMessage(config, buffer, msgOffset, payloadLength);
                    break;
                case 8:
                    HandleCloseMessage(config, buffer, msgOffset, payloadLength);
                    break;
            }

            return true;
        }

        static void HandleArrayMessage(Config config, byte[] buffer, int msgOffset, int payloadLength)
        {
            (Connection conn, int _, bool expectMask, ConcurrentQueue<Message> queue, Action<Connection> _, BufferPool bufferPool) = config;

            ArrayBuffer arrayBuffer = bufferPool.Take(payloadLength);

            if (expectMask)
            {
                int maskOffset = msgOffset - Constants.MaskSize;
                // write the result of toggle directly into arrayBuffer to avoid 2nd copy call
                MessageProcessor.ToggleMask(buffer, msgOffset, arrayBuffer, payloadLength, buffer, maskOffset);
            }
            else
            {
                arrayBuffer.CopyFrom(buffer, msgOffset, payloadLength);
            }

            // dump after mask off
            Log.DumpBuffer($"Message", arrayBuffer);

            queue.Enqueue(new Message(conn.connId, arrayBuffer));
        }

        static void HandleCloseMessage(Config config, byte[] buffer, int msgOffset, int payloadLength)
        {
            (Connection conn, int _, bool expectMask, ConcurrentQueue<Message> _, Action<Connection> closeCallback, BufferPool _) = config;

            if (expectMask)
            {
                int maskOffset = msgOffset - Constants.MaskSize;
                MessageProcessor.ToggleMask(buffer, msgOffset, payloadLength, buffer, maskOffset);
            }

            // dump after mask off
            Log.DumpBuffer($"Message", buffer, msgOffset, payloadLength);

            Log.Info($"Close: {GetCodeCode(buffer, msgOffset)} message:{GetCloseMessage(buffer, msgOffset, payloadLength)}");

            closeCallback.Invoke(conn);
        }

        static string GetCloseMessage(byte[] buffer, int msgOffset, int payloadLength)
        {
            return Encoding.UTF8.GetString(buffer, msgOffset + 2, payloadLength - 2);
        }

        static int GetCodeCode(byte[] buffer, int msgOffset)
        {
            return buffer[msgOffset + 0] << 8 | buffer[msgOffset + 1];
        }
    }
}
