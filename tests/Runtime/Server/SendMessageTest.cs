using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Mirror.SimpleWeb.Tests.Server
{
    [Category("SimpleWebTransport")]
    public class SendMessageTest : SimpleWebTestBase
    {
        protected override bool StartServer => true;

        [UnityTest]
        public IEnumerator SendOne()
        {
            Task<RunNode.Result> task = RunNode.RunAsync("ReceiveMessages.js");

            yield return server.WaitForConnection;

            byte[] bytes = new byte[] { 1, 2, 3, 4, 5 };
            ArraySegment<byte> segment = new ArraySegment<byte>(bytes);

            server.ServerSend(new List<int> { 1 }, Channels.DefaultReliable, segment);

            yield return new WaitForSeconds(0.5f);
            server.ServerDisconnect(1);

            yield return new WaitUntil(() => task.IsCompleted);

            RunNode.Result result = task.Result;

            result.AssetTimeout(false);
            result.AssetOutput(
                "length: 5 msg: 01 02 03 04 05"
                );
            result.AssetErrors();
        }

        [UnityTest]
        [TestCase(1, ExpectedResult = default)]
        [TestCase(50, ExpectedResult = default)]
        [TestCase(100, ExpectedResult = default)]
        [TestCase(124, ExpectedResult = default)]
        [TestCase(125, ExpectedResult = default)]
        [TestCase(126, ExpectedResult = default)]
        [TestCase(127, ExpectedResult = default)]
        [TestCase(128, ExpectedResult = default)]
        [TestCase(129, ExpectedResult = default)]
        [TestCase(250, ExpectedResult = default)]
        [TestCase(1000, ExpectedResult = default)]
        [TestCase(5000, ExpectedResult = default)]
        public IEnumerator SendDifferentSizes(int msgSize)
        {
            Task<RunNode.Result> task = RunNode.RunAsync("ReceiveMessages.js");

            yield return server.WaitForConnection;

            byte[] bytes = Enumerable.Range(1, msgSize).Select(x => (byte)x).ToArray();
            ArraySegment<byte> segment = new ArraySegment<byte>(bytes);

            server.ServerSend(new List<int> { 1 }, Channels.DefaultReliable, segment);

            yield return new WaitForSeconds(0.5f);
            server.ServerDisconnect(1);

            yield return new WaitUntil(() => task.IsCompleted);

            RunNode.Result result = task.Result;

            result.AssetTimeout(false);
            result.AssetOutput(
                $"length: {msgSize} msg: {BitConverter.ToString(bytes).Replace('-', ' ')}"
                );
            result.AssetErrors();
        }

        [UnityTest]
        public IEnumerator SendMany()
        {
            Task<RunNode.Result> task = RunNode.RunAsync("ReceiveManyMessages.js", 5_000);

            yield return server.WaitForConnection;
            const int messageCount = 100;

            for (int i = 0; i < messageCount; i++)
            {
                using (PooledNetworkWriter writer = NetworkWriterPool.GetWriter())
                {
                    writer.WriteByte((byte)i);
                    writer.WriteInt32(100);

                    ArraySegment<byte> segment = writer.ToArraySegment();

                    server.ServerSend(new List<int> { 1 }, Channels.DefaultReliable, segment);
                }
            }

            yield return new WaitForSeconds(1);
            server.ServerDisconnect(1);

            yield return new WaitUntil(() => task.IsCompleted);

            RunNode.Result result = task.Result;

            string expectedFormat = "length: 5 msg: {0:X2} 64 00 00 00";
            IEnumerable<string> expected = Enumerable.Range(0, messageCount).Select(i => string.Format(expectedFormat, i));

            result.AssetTimeout(false);
            result.AssetOutput(expected.ToArray());
            result.AssetErrors();
        }

        [UnityTest]
        public IEnumerator ErrorWhenMessageTooBig()
        {
            yield return null;

            ArraySegment<byte> segment = new ArraySegment<byte>(new byte[70_000]);

            LogAssert.Expect(LogType.Error, "Message greater than max size");
            bool result = server.ServerSend(new List<int> { 1 }, Channels.DefaultReliable, segment);

            Assert.IsFalse(result);
        }

        [UnityTest]
        public IEnumerator ErrorWhenMessageTooSmall()
        {
            yield return null;

            ArraySegment<byte> segment = new ArraySegment<byte>();

            LogAssert.Expect(LogType.Error, "Message count was zero");
            bool result = server.ServerSend(new List<int> { 1 }, Channels.DefaultReliable, segment);

            Assert.IsFalse(result);
        }
    }
}
