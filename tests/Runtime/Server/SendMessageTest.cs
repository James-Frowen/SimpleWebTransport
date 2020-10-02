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

            result.AssetTimeout(false);
            result.AssetOutput(
                "length: 5 msg: 01 02 03 04 05"
                );
            result.AssetErrors();
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

            string expectedFormat = "length: 5 msg: {0:X2} 64 00 00 00";
            IEnumerable<string> expected = Enumerable.Range(0, messageCount).Select(i => string.Format(expectedFormat, i));

            result.AssetTimeout(false);
            result.AssetOutput(expected.ToArray());
            result.AssetErrors();
        }
    }
}
