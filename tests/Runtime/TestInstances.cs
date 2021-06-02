using System;
using System.Collections.Generic;
using UnityEngine;

namespace JamesFrowen.SimpleWeb.Tests
{
    public static class SimpleWebTransportExtension
    {
        /// <summary>
        /// Create copy of array, need to do this because buffers are re-used
        /// </summary>
        /// <param name="_"></param>
        /// <param name="segment"></param>
        /// <returns></returns>
        public static byte[] CreateCopy(this SimpleWebTransport _, ArraySegment<byte> segment)
        {
            byte[] copy = new byte[segment.Count];
            Array.Copy(segment.Array, segment.Offset, copy, 0, segment.Count);
            return copy;
        }
    }
    interface NeedInitTestInstance
    {
        void Init();
    }
    public class ServerTestInstance : SimpleWebTransport, NeedInitTestInstance
    {
        public readonly List<int> onConnect = new List<int>();
        public readonly List<int> onDisconnect = new List<int>();
        public readonly List<(int connId, byte[] data)> onData = new List<(int connId, byte[] data)>();
        public readonly List<(int connId, Exception exception)> onError = new List<(int connId, Exception exception)>();

        public void Init()
        {
#if MIRROR_29_0_OR_NEWER
            base.OnServerConnected = (connId) => onConnect.Add(connId);
            base.OnServerDisconnected = (connId) => onDisconnect.Add(connId);
            base.OnServerDataReceived = (connId, data, _) => onData.Add((connId, this.CreateCopy(data)));
            base.OnServerError = (connId, exception) => onError.Add((connId, exception));
#else
            base.OnServerConnected.AddListener((connId) => onConnect.Add(connId));
            base.OnServerDisconnected.AddListener((connId) => onDisconnect.Add(connId));
            base.OnServerDataReceived.AddListener((connId, data, _) => onData.Add((connId, this.CreateCopy(data))));
            base.OnServerError.AddListener((connId, exception) => onError.Add((connId, exception)));
#endif
        }

        public WaitUntil WaitForConnection => new WaitUntil(() => onConnect.Count >= 1);

#if MIRROR_26_0_OR_NEWER
        public void ServerSend(System.Collections.Generic.List<int> connectionIds, int channelId, ArraySegment<byte> segment)
        {
            foreach (int id in connectionIds)
            {
                ServerSend(id, channelId, segment);
            }
        }
#endif
    }
    public class ClientTestInstance : SimpleWebTransport, NeedInitTestInstance
    {
        public int onConnect = 0;
        public int onDisconnect = 0;
        public readonly List<byte[]> onData = new List<byte[]>();
        public readonly List<Exception> onError = new List<Exception>();

        public void Init()
        {
#if MIRROR_29_0_OR_NEWER
            base.OnClientConnected = () => onConnect++;
            base.OnClientDisconnected = () => onDisconnect++;
            base.OnClientDataReceived = (data, _) => onData.Add(this.CreateCopy(data));
            base.OnClientError = (exception) => onError.Add(exception);
#else
            base.OnClientConnected.AddListener(() => onConnect++);
            base.OnClientDisconnected.AddListener(() => onDisconnect++);
            base.OnClientDataReceived.AddListener((data, _) => onData.Add(this.CreateCopy(data)));
            base.OnClientError.AddListener((exception) => onError.Add(exception));
#endif
        }

        public WaitUntil WaitForConnect => new WaitUntil(() => onConnect >= 1);
    }
}
