using System;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;

namespace Mirror.SimpleWeb
{
    internal class WebSocketClientWebGl : IWebSocketClient
    {
        static WebSocketClientWebGl instance;
#if UNITY_WEBGL && !UNITY_EDITOR
        internal WebSocketClientWebGl() { instance = this; }
#else
        internal WebSocketClientWebGl() => throw new NotSupportedException();
#endif

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
            // disconnect should cause closeCallback and OnDisconnect to be called
            SimpleWebJLib.Disconnect();
        }

        private void OnDisconnect()
        {
            instance.onDisconnect?.Invoke();
            IsConnected = false;
            SimpleWebClient.RemoveInstance();
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
            instance.OnDisconnect();
        }

        [MonoPInvokeCallback(typeof(Action<IntPtr, int>))]
        static void MessageCallback(IntPtr bufferPtr, int count)
        {
            try
            {
                byte[] buffer = new byte[count];
                Marshal.Copy(bufferPtr, buffer, 0, count);

                instance.onData?.Invoke(new ArraySegment<byte>(buffer, 0, count));
            }
            catch (Exception e)
            {
                Debug.LogError($"onData {e.GetType()}: {e.Message}\n{e.StackTrace}");
                instance.onError?.Invoke();
            }
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
