#define SIMPLE_WEB_INFO_LOG
using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;

namespace Mirror.SimpleWeb
{
    [Serializable]
    public struct SslConfig
    {
        public bool enabled;
        public string certPath;
    }
    internal class SslHelper
    {
        readonly SslConfig sslConfig;
        readonly X509Certificate2 certificate;

        public SslHelper(SslConfig sslConfig)
        {
            this.sslConfig = sslConfig;
            if (sslConfig.enabled)
                certificate = new X509Certificate2(sslConfig.certPath, string.Empty);
        }

        internal bool TryCreateStream(Connection conn)
        {
            NetworkStream stream = conn.client.GetStream();
            if (sslConfig.enabled)
            {
                try
                {
                    conn.stream = CreateStream(stream);
                    return true;
                }
                catch (Exception e)
                {
                    Log.Error($"Create SSLStream Failed: {e}", false);
                    return false;
                }
            }
            else
            {
                conn.stream = stream;
                return true;
            }
        }

        Stream CreateStream(NetworkStream stream)
        {
            // dont need RemoteCertificateValidationCallback for server stream
            SslProtocols protocols = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls;
            SslStream sslStream = new SslStream(stream, true, acceptClient);
            sslStream.AuthenticateAsServer(certificate, false, protocols, false);

            return sslStream;
        }

        bool acceptClient(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            // always accept client
            return true;
        }

        bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;

            Debug.LogErrorFormat("Certificate error: {0}", sslPolicyErrors);
            return false;
        }
    }
}
