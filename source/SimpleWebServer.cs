using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mirror.SimpleWeb
{
    public class SimpleWebServer
    {
        const int RecieveLoopSleepTime = 1;

        readonly short port;

        readonly WebSocketServer server;

        public SimpleWebServer(short port, bool noDelay, int sendTimeout, int receiveTimeout)
        {
            this.port = port;

            server = new WebSocketServer(noDelay, sendTimeout, receiveTimeout, RecieveLoopSleepTime);
        }

        public bool Active { get; private set; }

        public event Action<int> onConnect;
        public event Action<int> onDisconnect;
        public event Action<int, ArraySegment<byte>> onData;
        public event Action<int> onError;

        public void Start()
        {
            server.Listen(port);
            Active = true;
        }

        public void Stop()
        {
            server.Stop();
            Active = false;
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

        public void Update(MonoBehaviour behaviour)
        {
            while (server.receiveQueue.TryDequeue(out Message next))
            {
                switch (next.type)
                {
                    case EventType.Connected:
                        onConnect?.Invoke(next.connId);
                        break;
                    case EventType.Data:
                        onData?.Invoke(next.connId, next.data);
                        break;
                    case EventType.Disconnected:
                        onDisconnect?.Invoke(next.connId);
                        break;
                }

                // return if behaviour was disabled after data
                if (!behaviour.enabled)
                    return;
            }
        }
    }
}
