using System;
using System.Collections.Generic;

namespace Mirror.SimpleWeb
{
    public class SimpleWebServer
    {
        const int RecieveLoopSleepTime = 1;

        readonly int sendTimeout;
        readonly bool noDelay;
        readonly short port;

        WebSocketServer server;

        public SimpleWebServer(short port, bool noDelay, int sendTimeout)
        {
            this.port = port;
            this.sendTimeout = sendTimeout;
            this.noDelay = noDelay;
        }

        public bool Active { get; internal set; }

        public event Action<int> onConnect;
        public event Action<int> onDisconnect;
        public event Action<int, ArraySegment<byte>> onData;
        public event Action<int> onError;

        internal void Start()
        {
            server = new WebSocketServer(noDelay, sendTimeout, RecieveLoopSleepTime);
            server.Listen(port);
        }

        internal void Stop()
        {
            server.Stop();
        }

        internal void SendAll(List<int> connectionIds, ArraySegment<byte> segment)
        {
            foreach (int id in connectionIds)
            {
                server.Send(id, segment);
            }
        }

        internal bool KickClient(int connectionId)
        {
            return server.CloseConnection(connectionId);
        }

        internal string GetClientAddress(int connectionId)
        {
            return server.GetClientAddress(connectionId);
        }
    }
}
