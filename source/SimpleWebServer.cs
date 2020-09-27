using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mirror.SimpleWeb
{
    public class SimpleWebServer
    {
        readonly short port;
        readonly int maxMessagesPerTick;

        readonly WebSocketServer server;

        public SimpleWebServer(short port, int maxMessagesPerTick, bool noDelay, int sendTimeout, int receiveTimeout, int maxMessageSize, SslConfig sslConfig)
        {
            this.port = port;
            this.maxMessagesPerTick = maxMessagesPerTick;

            server = new WebSocketServer(noDelay, sendTimeout, receiveTimeout, maxMessageSize, sslConfig);
        }

        public bool Active { get; private set; }

        public event Action<int> onConnect;
        public event Action<int> onDisconnect;
        public event Action<int, ArraySegment<byte>> onData;
        public event Action<int, Exception> onError;

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

        public void ProcessMessageQueue(MonoBehaviour behaviour)
        {
            int processedCount = 0;
            // check enabled every time incase behaviour was disabled after data
            while (
                behaviour.enabled &&
                server.receiveQueue.TryDequeue(out Message next) &&
                processedCount < maxMessagesPerTick
                )
            {
                processedCount++;

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
                    case EventType.Error:
                        onError?.Invoke(next.connId, next.exception);
                        break;
                }
            }
        }
    }
}
