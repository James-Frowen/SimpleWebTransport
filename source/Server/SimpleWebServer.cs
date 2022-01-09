using System;
using System.Collections.Generic;
using UnityEngine;

namespace JamesFrowen.SimpleWeb
{
    public class SimpleWebServer
    {
        readonly int maxMessagesPerTick;

        readonly WebSocketServer server;
        readonly BufferPool bufferPool;
        readonly int maxMessageSize;

        public SimpleWebServer(int maxMessagesPerTick, TcpConfig tcpConfig, int maxMessageSize, int handshakeMaxSize, SslConfig sslConfig)
        {
            this.maxMessagesPerTick = maxMessagesPerTick;
            // use max because bufferpool is used for both messages and handshake
            int max = Math.Max(maxMessageSize, handshakeMaxSize);
            bufferPool = new BufferPool(5, 20, max);
            this.maxMessageSize = maxMessageSize;

            server = new WebSocketServer(tcpConfig, maxMessageSize, handshakeMaxSize, sslConfig, bufferPool);
        }

        public bool Active { get; private set; }

        public event Action<int> onConnect;
        public event Action<int> onDisconnect;
        public event Action<int, ArraySegment<byte>> onData;
        public event Action<int, Exception> onError;

        public void Start(ushort port)
        {
            server.Listen(port);
            Active = true;
        }

        public void Stop()
        {
            server.Stop();
            Active = false;
        }

        public void SendAll(List<int> connectionIds, ArraySegment<byte> source)
        {
            ArrayBuffer buffer = bufferPool.Take(source.Count);
            buffer.CopyFrom(source);
            buffer.SetReleasesRequired(connectionIds.Count);

            // make copy of array before for each, data sent to each client is the same
            foreach (int id in connectionIds)
            {
                server.Send(id, buffer);
            }
        }

        public void SendOne(int connectionId, ArraySegment<byte> source)
        {
            ArrayBuffer buffer = bufferPool.Take(source.Count);
            buffer.CopyFrom(source);

            server.Send(connectionId, buffer);
        }

        /// <summary>
        /// Sends a large message on main thread, this is blocking till message is sent
        /// </summary>
        /// <param name="id"></param>
        /// <param name="segment"></param>
        public void SendLargeMessage(int connectionId, ArraySegment<byte> segment)
        {
            server.SendLargeMessage(connectionId, segment);
        }

        public bool KickClient(int connectionId)
        {
            return server.CloseConnection(connectionId);
        }

        public string GetClientAddress(int connectionId)
        {
            return server.GetClientAddress(connectionId);
        }

        /// <summary>
        /// Processes all new messages
        /// </summary>
        public void ProcessMessageQueue()
        {
            ProcessMessageQueue(null);
        }

        /// <summary>
        /// Processes all messages while <paramref name="behaviour"/> is enabled
        /// </summary>
        /// <param name="behaviour"></param>
        public void ProcessMessageQueue(MonoBehaviour behaviour)
        {
            int processedCount = 0;
            bool skipEnabled = behaviour == null;
            // check enabled every time incase behaviour was disabled after data
            while (
                (skipEnabled || behaviour.enabled) &&
                processedCount < maxMessagesPerTick &&
                // Dequeue last
                server.receiveQueue.TryDequeue(out Message next)
                )
            {
                processedCount++;

                switch (next.type)
                {
                    case EventType.Connected:
                        onConnect?.Invoke(next.connId);
                        break;
                    case EventType.Data:
                        onData?.Invoke(next.connId, next.data.ToSegment());
                        next.data.Release();
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

        /// <summary>
        /// Allows large messages from connection
        /// <para>WARNING: large message will cause buffers to be allocated which may cause negative performance</para>
        /// </summary>
        /// <param name="connectionId"></param>
        /// <param name="enabled"></param>
        public void AllowLargeMessage(int connectionId, bool enabled)
        {
            server.AllowLargeMessage(connectionId, enabled);
        }
    }
}
