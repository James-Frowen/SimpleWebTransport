using System;
using AOT;

namespace Mirror.SimpleWeb
{
    public class SimpleWebClient
    {
        public bool IsConnected() => SimpleWebJLib.IsConnected();

        public event Action onConnect;
        public event Action onDisconnect;
        public event Action<ArraySegment<byte>> onData;
        public event Action onError;

        public void Connect(string address)
        {
            SimpleWebJLib.Connect(address, OpenCallback, CloseCallBack, MessageCallback, ErrorCallback);
        }

        public void Disconnect()
        {
            SimpleWebJLib.Disconnect();
        }

        public void Send(ArraySegment<byte> segment)
        {
            SimpleWebJLib.Send(segment.Array, 0, segment.Count);
        }

        [MonoPInvokeCallback(typeof(Action))]
        void OpenCallback()
        {
            onConnect?.Invoke();
        }

        [MonoPInvokeCallback(typeof(Action))]
        void CloseCallBack()
        {
            onDisconnect?.Invoke();
        }

        [MonoPInvokeCallback(typeof(Action<byte, int>))]
        void MessageCallback(byte[] data, int count)
        {
            onData?.Invoke(new ArraySegment<byte>(data, 0, count));
        }

        [MonoPInvokeCallback(typeof(Action))]
        void ErrorCallback()
        {
            onError?.Invoke();
        }
    }
}
