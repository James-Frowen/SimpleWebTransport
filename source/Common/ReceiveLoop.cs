using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Mirror.SimpleWeb
{
    internal static class ReceiveLoop
    {
        public static void Loop(Connection conn, int maxMessageSize, bool expectMask, ConcurrentQueue<Message> queue, Action<Connection> closeCallback, BufferPool bufferPool)
        {
            byte[] readBuffer = new byte[Constants.HeaderSize + (expectMask ? Constants.MaskSize : 0) + maxMessageSize];
            try
            {
                try
                {
                    TcpClient client = conn.client;
                    Stream stream = conn.stream;

                    while (client.Connected)
                    {
                        bool success = ReadOneMessage(queue, closeCallback, conn, stream, readBuffer, maxMessageSize, expectMask, bufferPool);
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
            catch (IOException e)
            {
                // this could happen if client disconnects
                Log.Warn($"SafeRead IOException\n{e.Message}", false);
            }
            catch (InvalidDataException e)
            {
                Log.Error($"Invalid data from {conn}: {e.Message}");
                queue.Enqueue(new Message(conn.connId, e));
            }
            catch (Exception e) { Debug.LogException(e); }
            finally
            {
                closeCallback.Invoke(conn);
            }
        }

        static bool ReadOneMessage(ConcurrentQueue<Message> queue, Action<Connection> closeCallback, Connection conn, Stream stream, byte[] buffer, int maxMessageSize, bool expectMask, BufferPool bufferPool)
        {
            Log.Verbose($"Message From {conn}");
            int offset = 0;
            // read 2
            offset = ReadHelper.Read(stream, buffer, offset, Constants.HeaderMinSize);


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
            if (expectMask)
            {
                int maskOffset = offset - payloadLength - Constants.MaskSize;
                MessageProcessor.ToggleMask(buffer, msgOffset, payloadLength, buffer, maskOffset);
            }

            // dump after mask off
            Log.DumpBuffer($"Raw Header", buffer, 0, msgOffset);
            Log.DumpBuffer($"Message", buffer, msgOffset, payloadLength);

            HandleMessage(queue, closeCallback, opcode, conn, buffer, msgOffset, payloadLength, bufferPool);
            return true;
        }

        static void HandleMessage(ConcurrentQueue<Message> queue, Action<Connection> closeCallback, int opcode, Connection conn, byte[] msg, int offset, int length, BufferPool bufferPool)
        {
            if (opcode == 2)
            {
                ArrayBuffer buffer = bufferPool.Take(length);

                buffer.CopyFrom(msg, offset, length);

                queue.Enqueue(new Message(conn.connId, buffer));
            }
            else if (opcode == 8)
            {
                Log.Info($"Close: {msg[offset + 0] << 8 | msg[offset + 1]} message:{Encoding.UTF8.GetString(msg, offset + 2, length - 2)}");
                closeCallback.Invoke(conn);
            }
        }
    }
}
