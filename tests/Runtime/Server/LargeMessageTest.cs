using System;
using System.Collections;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace JamesFrowen.SimpleWeb.Tests.Server
{
    [Category("SimpleWebTransport")]
    public class LargeMessageTest : SimpleWebTestBase
    {
        protected override bool StartServer => true;

        [UnityTest]
        public IEnumerator SendLarge()
        {
            Task<RunNode.Result> task = RunNode.RunAsync("ReceiveMessages.js");

            yield return server.WaitForConnection;

            byte[] bytes = new byte[80_000];
            var random = new System.Random();
            random.NextBytes(bytes);

            var segment = new ArraySegment<byte>(bytes);

            server.server.AllowLargeMessage(1, true);
            server.server.SendLargeMessage(1, segment);

            yield return new WaitForSeconds(0.5f);
            server.ServerDisconnect(1);

            yield return new WaitUntil(() => task.IsCompleted);

            RunNode.Result result = task.Result;

            var stringBuilder = new StringBuilder();
            stringBuilder.Append("length: 80000 msg:");
            for (int i = 0; i < 80_000; i++)
            {
                stringBuilder.Append(" ");
                stringBuilder.Append(bytes[i].ToString("X2"));
            }
            string output = stringBuilder.ToString();

            result.AssetTimeout(false);
            result.AssetOutput(output);
            result.AssetErrors();
        }

        [UnityTest]
        public IEnumerator SendLargeError()
        {
            Task<RunNode.Result> task = RunNode.RunAsync("ReceiveMessages.js");

            yield return server.WaitForConnection;

            byte[] bytes = new byte[80_000];
            var random = new System.Random();
            random.NextBytes(bytes);

            var segment = new ArraySegment<byte>(bytes);

            var expected = new InvalidOperationException("Large message is disabled set AllowLargeMessage to true first");
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            {
                server.server.SendLargeMessage(1, segment);
            });
            Assert.That(exception.Message, Is.EqualTo(expected.Message));

            server.ServerDisconnect(1);
            yield return new WaitUntil(() => task.IsCompleted);

            RunNode.Result result = task.Result;
            result.AssetTimeout(false);
            // no messages
            result.AssetOutput();
            result.AssetErrors();
        }


        [UnityTest]
        public IEnumerator ReceiveLargeArrayFromServer()
        {
            // IMPORTANT: cant use javascript here because it will fragment the message instead of using longer header
            var tcpConfig = new TcpConfig(false, timeout, timeout);
            var client = SimpleWebClient.Create(5000, server.maxMessageSize, tcpConfig);
            client.Connect(new UriBuilder
            {
                Scheme = "ws",
                Host = "localhost",
                Port = 7776
            }.Uri);

            yield return server.WaitForConnection;
            server.server.AllowLargeMessage(1, true);
            client.AllowLargeMessage(true);
            ArraySegment<byte> clientReceive;
            client.onData += (s) => clientReceive = s;

            byte[] bytes = new byte[80_000];
            var random = new System.Random();
            random.NextBytes(bytes);
            server.server.SendLargeMessage(1, new ArraySegment<byte>(bytes));

            // wait for message
            float end = Time.time + 2;
            while (end > Time.time)
            {
                client.ProcessMessageQueue();
                yield return null;
            }

            Assert.That(clientReceive.Array, Is.Not.Null);
            Assert.That(clientReceive.Count, Is.EqualTo(80_000));

            int offset = clientReceive.Offset;
            byte[] array = clientReceive.Array;
            for (int i = 0; i < 80_000; i++)
            {
                if (bytes[i] != array[i + offset])
                {
                    Assert.Fail("data not the same");
                }
            }
        }


        [UnityTest]
        public IEnumerator ReceiveLargeArrayFromClient()
        {
            // IMPORTANT: cant use javascript here because it will fragment the message instead of using longer header
            var tcpConfig = new TcpConfig(false, timeout, timeout);
            var client = SimpleWebClient.Create(5000, server.maxMessageSize, tcpConfig) as WebSocketClientStandAlone;
            client.Connect(new Uri("ws://localhost:7776"));

            yield return server.WaitForConnection;
            server.server.AllowLargeMessage(1, true);
            client.AllowLargeMessage(true);

            byte[] bytes = new byte[80_000];
            var random = new System.Random();
            random.NextBytes(bytes);
            client.SendLargeMessage(new ArraySegment<byte>(bytes));

            // wait for message
            yield return new WaitForSeconds(2f);

            Assert.That(server.onData, Has.Count.EqualTo(1), $"Should have 1 message");
            (int connId, byte[] data) = server.onData[0];
            Assert.That(connId, Is.EqualTo(1), "Connd id should be 1");

            Assert.That(data.Length, Is.EqualTo(80_000), "Should have 80_000 bytes");
            for (int i = 0; i < 80_000; i++)
            {
                if (bytes[i] != data[i])
                {
                    Assert.Fail("data not the same");
                }
            }
        }
    }
}
