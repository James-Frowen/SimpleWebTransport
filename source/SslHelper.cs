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
    public struct SslConfig
    {
        public bool enabled;
        public string certPath;
        public string certPassword;
        public SslProtocols sslProtocols;
    }
    internal class SslHelper
    {
        readonly SslConfig config;
        readonly X509Certificate2 certificate;

        public SslHelper(SslConfig sslConfig)
        {
            config = sslConfig;
            if (config.enabled)
                certificate = new X509Certificate2(config.certPath, config.certPassword);
        }

        internal bool TryCreateStream(Connection conn)
        {
            NetworkStream stream = conn.client.GetStream();
            if (config.enabled)
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
            SslStream sslStream = new SslStream(stream, true, acceptClient);
            sslStream.AuthenticateAsServer(certificate, false, config.sslProtocols, false);

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
