#define SIMPLE_WEB_INFO_LOG
using System;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using Debug = UnityEngine.Debug;

namespace Mirror.SimpleWeb
{
    internal class HandShake
    {
        private const int ResponseLength = 129;
        private const int KeyLength = 24;
        const string KeyHeaderString = "Sec-WebSocket-Key: ";
        const string HandshakeGUID = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

        readonly object lockObj = new object();
        readonly byte[] readBuffer = new byte[300];
        readonly byte[] keyBuffer = new byte[60];
        readonly byte[] response = new byte[ResponseLength];
        readonly SHA1 sha1 = SHA1.Create();

        public HandShake()
        {
            Encoding.UTF8.GetBytes(HandshakeGUID, 0, HandshakeGUID.Length, keyBuffer, KeyLength);
        }
        ~HandShake()
        {
            sha1.Dispose();
        }

        /// <summary>
        /// Clears buffers so that data can't be used by next request
        /// </summary>
        void ClearBuffers()
        {
            Array.Clear(readBuffer, 0, 300);
            Array.Clear(readBuffer, 0, 24);
            Array.Clear(response, 0, ResponseLength);
        }

        public bool TryHandshake(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            try
            {
                byte[] getHeader = new byte[3];
                bool success = ReadHelper.SafeRead(stream, getHeader, 0, 3);
                if (!success)
                    return false;

                if (!IsGet(getHeader))
                    return false;
            }
            catch (Exception e) { Debug.LogException(e); return false; }

            // lock so that buffers can only be used by this thread
            lock (lockObj)
            {
                try
                {
                    int length = client.Available;
                    bool success = ReadHelper.SafeRead(stream, readBuffer, 0, length);
                    if (!success)
                        return false;

                    string msg = Encoding.UTF8.GetString(readBuffer, 0, length);

                    AcceptHandshake(stream, msg);
                    return true;
                }
                catch (Exception e) { Debug.LogException(e); return false; }
                finally
                {
                    ClearBuffers();
                }
            }
        }

        bool IsGet(byte[] getHeader)
        {
            // just check bytes here instead of using Encoding.UTF8
            return getHeader[0] == 71 && // G
                   getHeader[1] == 69 && // E
                   getHeader[2] == 84;   // T
        }

        void AcceptHandshake(NetworkStream stream, string msg)
        {
            CreateResponse(msg);

            stream.Write(response, 0, ResponseLength);
            Log.Info("Sent Handshake");
        }

        void CreateResponse(string msg)
        {
            GetKey(msg, keyBuffer);

            byte[] keyHash = sha1.ComputeHash(keyBuffer);

            string keyHashString = Convert.ToBase64String(keyHash);
            // compiler should merge these strings into 1 string before format
            string message = string.Format(
                "HTTP/1.1 101 Switching Protocols\r\n" +
                "Connection: Upgrade\r\n" +
                "Upgrade: websocket\r\n" +
                "Sec-WebSocket-Accept: {0}\r\n\r\n",
                keyHashString);

            Encoding.UTF8.GetBytes(message, 0, ResponseLength, response, 0);
        }

        static void GetKey(string msg, byte[] keyBuffer)
        {
            int start = msg.IndexOf(KeyHeaderString) + KeyHeaderString.Length;

            Encoding.UTF8.GetBytes(msg, start, KeyLength, keyBuffer, 0);
        }
    }
}
