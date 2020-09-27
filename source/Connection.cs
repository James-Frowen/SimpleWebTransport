#define SIMPLE_WEB_INFO_LOG
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace Mirror.SimpleWeb
{
    internal class Connection
    {
        public object lockObj = new object();
        public bool hasClosed;
        public int connId = -1;
        public TcpClient client;
        public Stream stream;
        public Thread receiveThread;
        public Thread sendThread;

        public ManualResetEvent sendPending = new ManualResetEvent(false);
        public ConcurrentQueue<ArraySegment<byte>> sendQueue = new ConcurrentQueue<ArraySegment<byte>>();

        /// <summary>
        /// disposes client and stops threads
        /// </summary>
        /// <returns>return true if closed by this call, false if was already closed</returns>
        public bool Close()
        {
            // check hasClosed first to stop ThreadInterruptedException on lock
            if (hasClosed) { return false; }

            lock (lockObj)
            {
                // check hasClosed again inside lock to make sure no other object has called this
                if (hasClosed) { return false; }
                hasClosed = true;

                // stop threads first so they dont try to use disposed objects
                receiveThread.Interrupt();
                sendThread?.Interrupt();

                stream.Dispose();
                stream = null;
                client.Dispose();
                client = null;

                return true;
            }
        }

        public override string ToString()
        {
            return $"[Conn:{connId}, endPoint:{client?.Client.RemoteEndPoint}]";
        }
    }
}
