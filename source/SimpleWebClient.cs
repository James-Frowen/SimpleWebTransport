using System;
using UnityEngine;

namespace Mirror.SimpleWeb
{
    public interface IWebSocketClient
    {
        event Action onConnect;
        event Action onDisconnect;
        event Action<ArraySegment<byte>> onData;
        event Action onError;

        bool IsConnected { get; }
        void Connect(string address);
        void Disconnect();
        void Send(ArraySegment<byte> segment);
    }

    public static class SimpleWebClient
    {
        static IWebSocketClient instance;

        public static IWebSocketClient Create(int maxMessageSize)
        {
            if (instance != null)
            {
                Debug.LogError("Cant create SimpleWebClient while one already exists");
                return null;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            instance = new WebSocketClientWebGl(maxMessageSize);
#else
            instance = new WebSocketClientStandAlone(maxMessageSize);
#endif
            return instance;
        }

        public static void CloseExisting()
        {
            instance?.Disconnect();
            instance = null;
        }

        /// <summary>
        /// Called by IWebSocketClient on disconnect
        /// </summary>
        internal static void RemoveInstance()
        {
            instance = null;
        }
    }
}
