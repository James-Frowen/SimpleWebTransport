using System;
using System.Runtime.InteropServices;
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

        public bool CheckJsConnected() => SimpleWebJSLib.IsConnected();
        public bool IsConnected { get; private set; }

        public event Action onConnect;
        public event Action onDisconnect;
        public event Action<ArraySegment<byte>> onData;
        public event Action onError;

        public void Connect(string address)
        {
            SimpleWebJSLib.Connect(address, OpenCallback, CloseCallBack, MessageCallback, ErrorCallback);
            IsConnected = true;
        }

        public void Disconnect()
        {
            SimpleWebJSLib.Disconnect();
            IsConnected = false;
        }

        public void Send(ArraySegment<byte> segment)
        {
            SimpleWebJSLib.Send(segment.Array, 0, segment.Count);
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

            SimpleWebJSLib.Disconnect();
            instance.IsConnected = false;
        }
    }
}
