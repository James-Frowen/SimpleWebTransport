using System;
using AOT;
using UnityEngine;

namespace Mirror.SimpleWeb
{
    public class SimpleWebClient
    {
        static SimpleWebClient instance;

        public static SimpleWebClient Create()
        {
            if (instance != null)
            {
                Debug.LogError("Cant create SimpleWebClient while one already exists");
                return null;
            }

            instance = new SimpleWebClient();
            return instance;
        }

        public static void CloseExisting()
        {
            if (instance == null)
            {
                Debug.LogError("Cant Close SimpleWebClient because none was open");
                return;
            }

            instance.Disconnect();
            instance = null;
        }

        // dont let others create new because only 1 instance can exist at once,
        // this is because callbacks sent to JS must be static
        private SimpleWebClient() { }

        public bool CheckJsConnected() => SimpleWebJLib.IsConnected();
        public bool IsConnected { get; private set; }

        public event Action onConnect;
        public event Action onDisconnect;
        public event Action<ArraySegment<byte>> onData;
        public event Action onError;

        public void Connect(string address)
        {
            SimpleWebJLib.Connect(address, OpenCallback, CloseCallBack, MessageCallback, ErrorCallback);
            IsConnected = true;
        }

        public void Disconnect()
        {
            SimpleWebJLib.Disconnect();
            IsConnected = false;
        }

        public void Send(ArraySegment<byte> segment)
        {
            SimpleWebJLib.Send(segment.Array, 0, segment.Count);
        }

        [MonoPInvokeCallback(typeof(Action))]
        static void OpenCallback()
        {
            instance.onConnect?.Invoke();
        }

        [MonoPInvokeCallback(typeof(Action))]
        static void CloseCallBack()
        {
            instance.onDisconnect?.Invoke();
        }

        [MonoPInvokeCallback(typeof(Action<byte, int>))]
        static void MessageCallback(byte[] data, int count)
        {
            instance.onData?.Invoke(new ArraySegment<byte>(data, 0, count));
        }

        [MonoPInvokeCallback(typeof(Action))]
        static void ErrorCallback()
        {
            instance.onError?.Invoke();

            SimpleWebJLib.Disconnect();
            instance.IsConnected = false;
        }
    }
}
