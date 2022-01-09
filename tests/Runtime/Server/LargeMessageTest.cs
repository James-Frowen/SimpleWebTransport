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
        public IEnumerator ReceiveLargeArrays()
        {
            ExpectInvalidDataError();

            // dont worry about result, run will timeout by itself
            _ = RunNode.RunAsync("SendLargeLargeMessagesArgs.js", args: new string[1] { "80000" });
            const int Length = 80000;

            yield return server.WaitForConnection;
            server.server.AllowLargeMessage(1, true);

            // wait for message
            yield return new WaitForSeconds(2f);

            Assert.That(server.onData, Has.Count.EqualTo(1), $"Should have 1 message");
            (int connId, byte[] data) = server.onData[0];


            Assert.That(connId, Has.Count.EqualTo(1), $"Should have id 1");
            Assert.That(data.Length, Is.EqualTo(Length));
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] != (byte)(i % 255))
                {
                    Assert.Fail("data did not match");
                }
            }

            Assert.That(server.onDisconnect, Has.Count.EqualTo(0), $"Should have 0 disconnect");
            Assert.That(server.onError, Has.Count.EqualTo(0), $"Should have 0 error");
        }
    }
}
