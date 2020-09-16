using System;
using System.Collections.Generic;

namespace Mirror.SimpleWeb
{
    public class SimpleWebServer
    {
        const int RecieveLoopSleepTime = 1;

        readonly short port;

        readonly WebSocketServer server;

        public SimpleWebServer(short port, bool noDelay, int sendTimeout)
        {
            this.port = port;

            server = new WebSocketServer(noDelay, sendTimeout, RecieveLoopSleepTime);
        }

        public bool Active { get; internal set; }

        public event Action<int> onConnect;
        public event Action<int> onDisconnect;
        public event Action<int, ArraySegment<byte>> onData;
        public event Action<int> onError;

        public void Start()
        {
            server.Listen(port);
        }

        public void Stop()
        {
            server.Stop();
        }

        public void SendAll(List<int> connectionIds, ArraySegment<byte> segment)
        {
            foreach (int id in connectionIds)
            {
                server.Send(id, segment);
            }
        }

        public bool KickClient(int connectionId)
        {
            return server.CloseConnection(connectionId);
        }

        public string GetClientAddress(int connectionId)
        {
            return server.GetClientAddress(connectionId);
        }

        public void Update()
        {
            while (server.receiveQueue.TryDequeue(out WebSocketServer.Message next))
            {
                switch (next.type)
                {
                    case WebSocketServer.EventType.Connected:
                        onConnect?.Invoke(next.connId);
                        break;
                    case WebSocketServer.EventType.Data:
                        onData?.Invoke(next.connId, next.data);
                        break;
                    case WebSocketServer.EventType.Disconnected:
                        onDisconnect?.Invoke(next.connId);
                        break;
                }
            }
        }
    }
}
