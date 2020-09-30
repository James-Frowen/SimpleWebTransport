using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Mirror.SimpleWeb
{
    internal class WebSocketClientStandAlone : IWebSocketClient
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        internal WebSocketClientStandAlone() => throw new NotSupportedException();
#else
        internal WebSocketClientStandAlone() { }
#endif

        public bool IsConnected { get; private set; }

        public event Action onConnect;
        public event Action onDisconnect;
        public event Action<ArraySegment<byte>> onData;
        public event Action onError;

        public void Connect(string address)
        {
            TcpClient client = new TcpClient();
            Uri uri = new Uri(address);
            client.Connect(uri.Host, uri.Port);

            Stream stream = client.GetStream();
            if (uri.Scheme == "wss")
            {
                SslStream sslStream = new SslStream(stream, true, ValidateServerCertificate);
                sslStream.AuthenticateAsClient(uri.Host);
                stream = sslStream;
            }

            byte[] keyBuffer = new byte[16];
            new System.Random().NextBytes(keyBuffer);

            string key = Convert.ToBase64String(keyBuffer);
            Debug.Log(key);
            string keySum = key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
            byte[] keySumBytes = Encoding.UTF8.GetBytes(keySum);
            byte[] keySumHash = SHA1.Create().ComputeHash(keySumBytes);

            string expectedResponse = Convert.ToBase64String(keySumHash);
            string handshake =
                $"GET /chat HTTP/1.1\r\n" +
                $"Host: {uri.Host}:{uri.Port}\r\n" +
                $"Upgrade: websocket\r\n" +
                $"Connection: Upgrade\r\n" +
                $"Sec-WebSocket-Key: {key}\r\n" +
                $"Sec-WebSocket-Version: 13\r\n" +
                "\r\n";
            byte[] encoded = Encoding.UTF8.GetBytes(handshake);
            stream.Write(encoded, 0, encoded.Length);

            byte[] responseBuffer = new byte[1000];

            int? lengthOrNull = ReadHelper.SafeReadTillMatch(stream, responseBuffer, 0, new byte[4] { (byte)'\r', (byte)'\n', (byte)'\r', (byte)'\n' });

            if (!lengthOrNull.HasValue)
            {
                Debug.LogError("Connected closed before handshake");
                stream.Dispose();
                client.Dispose();
                return;
            }

            string responseString = Encoding.UTF8.GetString(responseBuffer, 0, lengthOrNull.Value);

            string acceptHeader = "Sec-WebSocket-Accept: ";
            int startIndex = responseString.IndexOf(acceptHeader) + acceptHeader.Length;
            int endIndex = responseString.IndexOf("\r\n", startIndex);
            string responseKey = responseString.Substring(startIndex, endIndex - startIndex);

            if (responseKey != expectedResponse)
            {
                Debug.LogError("Reponse key incorrect");
                stream.Dispose();
                client.Dispose();
                return;
            }

            Debug.LogError("Success");
            Thread.Sleep(2000);


            stream.Dispose();
            client.Dispose();

            // recieveLoop
            // sendLoop
        }
        static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                return true;
            }

            // Do not allow this client to communicate with unauthenticated servers.
            return false;
        }
        public void Disconnect()
        {
            throw new NotImplementedException();
        }

        public void Send(ArraySegment<byte> segment)
        {
            throw new NotImplementedException();
        }
    }
}
