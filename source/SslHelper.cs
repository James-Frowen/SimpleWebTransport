#define SIMPLE_WEB_INFO_LOG
using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Mirror.SimpleWeb
{
    [Serializable]
    public struct SslConfig
    {
        public bool enabled;
        public string certPath;
        //public string certPassword;
        public bool ClientCertificateRequired;
        public SslProtocols EnabledSslProtocols;
        public bool CheckCertificateRevocation;
    }
    internal static class SslHelper
    {
        internal static bool TryCreateStream(Connection conn, SslConfig sslConfig)
        {
            try
            {
                NetworkStream stream = conn.client.GetStream();
                if (sslConfig.enabled)
                {
                    conn.stream = CreateStream(stream, sslConfig);
                    return true;
                }
                else
                {
                    conn.stream = stream;
                    return true;
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
                return false;
            }
        }

        static Stream CreateStream(NetworkStream stream, SslConfig sslConfig)
        {
            //string certPath = "./certs/mirrorTest.pfx";
            X509Certificate2 certificate = new X509Certificate2(sslConfig.certPath, string.Empty);


            // dont need RemoteCertificateValidationCallback for server stream
            SslStream sslStream = new SslStream(stream, true, callback);
            sslStream.AuthenticateAsServer(certificate, false, sslConfig.EnabledSslProtocols, false);


            return sslStream;
        }

        private static bool callback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
    }
}
