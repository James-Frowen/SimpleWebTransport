using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;

namespace Mirror.SimpleWeb
{
    internal class WebSocketClientWebGl : WebSocketClientBase, IWebSocketClient
    {
        public readonly Queue<Message> receiveQueue = new Queue<Message>();

        static WebSocketClientWebGl instance;

        readonly int maxMessageSize;
        readonly int maxMessagesPerTick;

        internal WebSocketClientWebGl(int maxMessageSize, int maxMessagesPerTick)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            instance = this;
            this.maxMessageSize = maxMessageSize;
            this.maxMessagesPerTick = maxMessagesPerTick;
#else
            throw new NotSupportedException();
#endif
        }

        public bool CheckJsConnected() => SimpleWebJSLib.IsConnected();
        public bool IsConnected { get; private set; }

        public event Action onConnect;
        public event Action onDisconnect;
        public event Action<ArraySegment<byte>> onData;
        public event Action<Exception> onError;

        public void Connect(string address)
        {
            SimpleWebJSLib.Connect(address, OpenCallback, CloseCallBack, MessageCallback, ErrorCallback);
            IsConnected = true;
        }

        public void Disconnect()
        {
            // disconnect should cause closeCallback and OnDisconnect to be called
            SimpleWebJSLib.Disconnect();
        }

        public void Send(ArraySegment<byte> segment)
        {
            if (segment.Count > maxMessageSize)
            {
                Debug.LogError($"Cant send message with length {segment.Count} because it is over the max size of {maxMessageSize}");
                return;
            }

            SimpleWebJSLib.Send(segment.Array, 0, segment.Count);
        }


        [MonoPInvokeCallback(typeof(Action))]
        static void OpenCallback()
        {
            instance.receiveQueue.Enqueue(new Message(EventType.Connected));
        }

        [MonoPInvokeCallback(typeof(Action))]
        static void CloseCallBack()
        {
            instance.receiveQueue.Enqueue(new Message(EventType.Disconnected));
            instance.IsConnected = false;
            SimpleWebClient.RemoveInstance();
        }

        [MonoPInvokeCallback(typeof(Action<IntPtr, int>))]
        static void MessageCallback(IntPtr bufferPtr, int count)
        {
            try
            {
                byte[] buffer = new byte[count];
                Marshal.Copy(bufferPtr, buffer, 0, count);

                ArraySegment<byte> segment = new ArraySegment<byte>(buffer, 0, count);
                instance.receiveQueue.Enqueue(new Message(segment));
            }
            catch (Exception e)
            {
                Debug.LogError($"onData {e.GetType()}: {e.Message}\n{e.StackTrace}");
                instance.onError?.Invoke(e);
            }
        }

        [MonoPInvokeCallback(typeof(Action))]
        static void ErrorCallback()
        {
            instance.receiveQueue.Enqueue(new Message(new Exception("Javascript Websocket error")));
            SimpleWebJSLib.Disconnect();
            instance.IsConnected = false;
        }

        public void ProcessMessageQueue(MonoBehaviour behaviour)
        {
            int processedCount = 0;
            // check enabled every time incase behaviour was disabled after data
            while (
                behaviour.enabled &&
                processedCount < maxMessagesPerTick &&
                // Dequeue last
                receiveQueue.Count > 0
                )
            {
                processedCount++;

                Message next = receiveQueue.Dequeue();
                switch (next.type)
                {
                    case EventType.Connected:
                        onConnect?.Invoke();
                        break;
                    case EventType.Data:
                        onData?.Invoke(next.data);
                        break;
                    case EventType.Disconnected:
                        onDisconnect?.Invoke();
                        break;
                    case EventType.Error:
                        onError?.Invoke(next.exception);
                        break;
                }
            }
        }
    }
}
