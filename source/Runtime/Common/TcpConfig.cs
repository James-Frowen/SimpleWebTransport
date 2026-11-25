using System.Net.Sockets;

namespace JamesFrowen.SimpleWeb
{
    [System.Serializable]
    public struct TcpConfig
    {
        public readonly bool noDelay;
#if UNITY_5_3_OR_NEWER
        [UnityEngine.Tooltip("in milliseconds, (0 means no timeout)")]
#endif
        public readonly int sendTimeout;
#if UNITY_5_3_OR_NEWER
        [UnityEngine.Tooltip("in milliseconds, (0 means no timeout)")]
#endif
        public readonly int receiveTimeout;

        public TcpConfig(bool noDelay, int sendTimeout, int receiveTimeout)
        {
            this.noDelay = noDelay;
            this.sendTimeout = sendTimeout;
            this.receiveTimeout = receiveTimeout;
        }

        public void ApplyTo(TcpClient client)
        {
            client.SendTimeout = sendTimeout;
            client.ReceiveTimeout = receiveTimeout;
            client.NoDelay = noDelay;
        }
    }
}
