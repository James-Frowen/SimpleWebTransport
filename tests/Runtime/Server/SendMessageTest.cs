using System;
using System.Collections;
using System.Collections.Generic;
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
        public IEnumerator SendFullArray()
        {
            Task<RunNode.Result> task = RunNode.RunAsync("ReceiveMessages.js");

            yield return WaitForConnect;

            byte[] bytes = new byte[] { 1, 2, 3, 4, 5 };
            ArraySegment<byte> segment = new ArraySegment<byte>(bytes, 0, 5);

            transport.ServerSend(new List<int> { 1 }, Channels.DefaultReliable, segment);

            yield return new WaitForSeconds(0.5f);
            transport.ServerDisconnect(1);

            yield return new WaitUntil(() => task.IsCompleted);

            RunNode.Result result = task.Result;

            string expected = $"length: {5} msg: 01 02 03 04 05";
            Assert.That(result.timedOut, Is.False, "js should close before timeout");
            Assert.That(result.output, Has.Length.EqualTo(1), "Should have 1 log");
            Assert.That(result.output[0], Is.EqualTo(expected), "Should have message log");
            Assert.That(result.error, Has.Length.EqualTo(0), "Should have no errors");
        }

        [UnityTest]
        public IEnumerator SendMany()
        {
            Task<RunNode.Result> task = RunNode.RunAsync("ReceiveManyMessages.js", 5_000);

            yield return WaitForConnect;
            const int messageCount = 100;

            for (int i = 0; i < messageCount; i++)
            {
                using (PooledNetworkWriter writer = NetworkWriterPool.GetWriter())
                {
                    writer.WriteByte((byte)i);
                    writer.WriteInt32(100);

                    ArraySegment<byte> segment = writer.ToArraySegment();

                    transport.ServerSend(new List<int> { 1 }, Channels.DefaultReliable, segment);
                }
            }

            yield return new WaitForSeconds(1);
            transport.ServerDisconnect(1);

            yield return new WaitUntil(() => task.IsCompleted);

            RunNode.Result result = task.Result;

            string expected = "length: 5 msg: {0:X2} 64 00 00 00";
            Assert.That(result.timedOut, Is.False, "js should close before timeout");
            Assert.That(result.output, Has.Length.EqualTo(messageCount), $"Should have {messageCount} logs");
            for (int i = 0; i < messageCount; i++)
            {
                Assert.That(result.output[i].ToLower(), Is.EqualTo(string.Format(expected, i).ToLower()), "Should have message log");
            }
            Assert.That(result.error, Has.Length.EqualTo(0), "Should have no errors");
        }
    }
}
