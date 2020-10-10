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
        public static void Loop(Connection conn, int maxMessageSize, bool expectMask, ConcurrentQueue<Message> queue, Action<Connection> closeCallback)
        {
            try
            {
                try
                {
                    TcpClient client = conn.client;
                    Stream stream = conn.stream;
                    byte[] headerBuffer = new byte[Constants.HeaderSize];

                    while (client.Connected)
                    {
                        bool success = ReadOneMessage(queue, closeCallback, conn, stream, headerBuffer, maxMessageSize, expectMask);
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

        static bool ReadOneMessage(ConcurrentQueue<Message> queue, Action<Connection> closeCallback, Connection conn, Stream stream, byte[] headerBuffer, int maxMessageSize, bool expectMask)
        {
            // read 2
            ReadHelper.Read(stream, headerBuffer, 0, Constants.HeaderMinSize);

            if (MessageProcessor.NeedToReadShortLength(headerBuffer))
            {
                ReadHelper.Read(stream, headerBuffer, Constants.HeaderMinSize, Constants.ShortLength);
            }

            MessageProcessor.ValidateHeader(headerBuffer, maxMessageSize, expectMask);

            byte[] maskBuffer = new byte[Constants.MaskSize];
            if (expectMask)
            {
                ReadHelper.Read(stream, maskBuffer, 0, Constants.MaskSize);
            }

            int opcode = MessageProcessor.GetOpcode(headerBuffer);
            int payloadLength = MessageProcessor.GetPayloadLength(headerBuffer);

            byte[] payload = new byte[payloadLength];
            ReadHelper.Read(stream, payload, 0, payloadLength);

            if (expectMask)
            {
                MessageProcessor.ToggleMask(payload, 0, payloadLength, maskBuffer, 0);
            }

            // dump after mask off
            Log.DumpBuffer($"Message From Client {conn}", payload, 0, payloadLength);

            HandleMessage(queue, closeCallback, opcode, conn, payload, 0, payloadLength);
            return true;
        }

        static void HandleMessage(ConcurrentQueue<Message> queue, Action<Connection> closeCallback, int opcode, Connection conn, byte[] buffer, int offset, int length)
        {
            if (opcode == 2)
            {
                ArraySegment<byte> data = new ArraySegment<byte>(buffer, offset, length);

                queue.Enqueue(new Message(conn.connId, data));
            }
            else if (opcode == 8)
            {
                Log.Info($"Close: {buffer[offset + 0] << 8 | buffer[offset + 1]} message:{Encoding.UTF8.GetString(buffer, offset + 2, length - 2)}");
                closeCallback.Invoke(conn);
            }
        }
    }
}
