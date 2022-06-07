using System.Text;
using NUnit.Framework;

namespace JamesFrowen.SimpleWeb.Tests
{
    [Category("SimpleWebTransport")]
    public class ServerHandshakeTest
    {
        const string KEY = "dGhlIHNhbXBsZSBub25jZQ==";
        const string HEADER_NORMAL =
            "GET /chat HTTP/1.1\r\n" +
            "Host: server.example.com\r\n" +
            "Upgrade: websocket\r\n" +
            "Connection: Upgrade\r\n" +
            "Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\n" +
            "Origin: http://example.com";
        const string HEADER_LOWER =
            "GET /chat HTTP/1.1\r\n" +
            "Host: server.example.com\r\n" +
            "Upgrade: websocket\r\n" +
            "Connection: Upgrade\r\n" +
            "sec-webSocket-key: dGhlIHNhbXBsZSBub25jZQ==\r\n" + // lower case here
            "Origin: http://example.com";

        const string HEADER_EXTRA =
            "GET /chat HTTP/1.1\r\n" +
            "Host: server.example.com\r\n" +
            "Upgrade: websocket\r\n" +
            "Connection: Upgrade\r\n" +
            "x-sec-webSocket-key: secret\r\n" + // not the real header, make sure it finds correct one
            "Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\n" +
            "Origin: http://example.com";

        static readonly byte[] expectedKey = new byte[ServerHandshake.KeyLength];
        static readonly byte[] actualyKey = new byte[ServerHandshake.KeyLength];

        [OneTimeSetUp]
        public void Setup()
        {
            Encoding.ASCII.GetBytes(KEY, 0, ServerHandshake.KeyLength, expectedKey, 0);
        }

        [Test]
        [TestCase(HEADER_NORMAL)]
        [TestCase(HEADER_LOWER)]
        [TestCase(HEADER_EXTRA)]
        public void FindsKeysFromMessage(string message)
        {
            ServerHandshake.GetKey(message, actualyKey);
            for (int i = 0; i < ServerHandshake.KeyLength; i++)
            {
                if (expectedKey[i] != actualyKey[i])
                    Assert.Fail($"Keys did not match.");
            }
        }
    }
}
