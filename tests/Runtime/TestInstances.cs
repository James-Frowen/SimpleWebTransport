using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mirror.SimpleWeb.Tests
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
            base.OnServerConnected.AddListener((connId) => onConnect.Add(connId));
            base.OnServerDisconnected.AddListener((connId) => onDisconnect.Add(connId));
            base.OnServerDataReceived.AddListener((connId, data, _) => onData.Add((connId, this.CreateCopy(data))));
            base.OnServerError.AddListener((connId, exception) => onError.Add((connId, exception)));
        }

        public WaitUntil WaitForConnection => new WaitUntil(() => onConnect.Count >= 1);
    }
    public class ClientTestInstance : SimpleWebTransport, NeedInitTestInstance
    {
        public int onConnect = 0;
        public int onDisconnect = 0;
        public readonly List<byte[]> onData = new List<byte[]>();
        public readonly List<Exception> onError = new List<Exception>();

        public void Init()
        {
            base.OnClientConnected.AddListener(() => onConnect++);
            base.OnClientDisconnected.AddListener(() => onDisconnect++);
            base.OnClientDataReceived.AddListener((data, _) => onData.Add(this.CreateCopy(data)));
            base.OnClientError.AddListener((exception) => onError.Add(exception));
        }

        public WaitUntil WaitForConnect => new WaitUntil(() => onConnect >= 1);
    }
}
