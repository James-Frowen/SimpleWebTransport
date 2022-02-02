using System;
#if UNITY_WEBGL
using System.Runtime.InteropServices;
#endif

namespace JamesFrowen.SimpleWeb
{
    public static class SimpleWebJSLib
    {
#if UNITY_WEBGL
        /// <summary>
        /// Call this before connecting to set up the jslib to work with unity version.
        /// </summary>
        /// <param name="unityVersion">major unity version, eg 2019</param>
        /// <returns></returns>
        [DllImport("__Internal")]
        public static extern bool Init(int unityVersion);

        [DllImport("__Internal")]
        internal static extern bool IsConnected(int index);

#pragma warning disable CA2101 // Specify marshaling for P/Invoke string arguments
        [DllImport("__Internal")]
#pragma warning restore CA2101 // Specify marshaling for P/Invoke string arguments
        internal static extern int Connect(string address, Action<int> openCallback, Action<int> closeCallBack, Action<int, IntPtr, int> messageCallback, Action<int> errorCallback);

        [DllImport("__Internal")]
        internal static extern void Disconnect(int index);

        [DllImport("__Internal")]
        internal static extern bool Send(int index, byte[] array, int offset, int length);
#else
        public static extern bool Init(int unityVersion) => throw new NotSupportedException();

        internal static bool IsConnected(int index) => throw new NotSupportedException();

        internal static int Connect(string address, Action<int> openCallback, Action<int> closeCallBack, Action<int, IntPtr, int> messageCallback, Action<int> errorCallback) => throw new NotSupportedException();

        internal static void Disconnect(int index) => throw new NotSupportedException();

        internal static bool Send(int index, byte[] array, int offset, int length) => throw new NotSupportedException();
#endif
    }
}
