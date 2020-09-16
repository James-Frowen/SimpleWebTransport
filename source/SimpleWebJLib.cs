using System;
using System.Runtime.InteropServices;

namespace Mirror.SimpleWeb
{
#if UNITY_WEBGL
    internal static class SimpleWebJLib
    {
        [DllImport("__Internal")]
        internal static extern bool IsConnected();

        [DllImport("__Internal", CharSet = CharSet.Unicode)]
        internal static extern void Connect(string address, Action openCallback, Action closeCallBack, Action<byte[], int> messageCallback, Action errorCallback);

        [DllImport("__Internal")]
        internal static extern void Disconnect();

        [DllImport("__Internal")]
        internal static extern bool Send(byte[] array, int offset, int length);
    }
#else
    internal static class SimpleWebJLib
    {
        internal static bool IsConnected() => throw new NotSupportedException();

        internal static void Connect(string address, Action openCallback, Action closeCallBack, Action<byte[], int> messageCallback, Action errorCallback) => throw new NotSupportedException();

        internal static void Disconnect() => throw new NotSupportedException();

        internal static bool Send(byte[] array, int offset, int length) => throw new NotSupportedException();
    }
#endif
}
