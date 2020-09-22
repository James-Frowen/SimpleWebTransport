using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Mirror.SimpleWeb.Tests.Server
{
    [Category("SimpleWebTransport")]
    public class ReceiveMessageTest : SimpleWebTestBase
    {
        protected override bool StartServer => true;

        [UnityTest]
        public IEnumerator SendFullArray()
        {
            // dont worry about result, run will timeout by itself
            _ = RunNode.RunAsync("SendMessages.js");

            yield return WaitForConnect;

            // wait for message
            yield return new WaitForSeconds(0.5f);

            Assert.That(onData, Has.Count.EqualTo(1), "Should have 1 message");
            Assert.That(onData[0].connId, Is.EqualTo(1), "Connd id should be 1");
            ArraySegment<byte> data = onData[0].data;
            Assert.That(data.Count, Is.EqualTo(10), "Shoudl have 10 bytes");
            for (int i = 0; i < 10; i++)
            {
                // js sends i+10 for each byte
                Assert.That(data.Array[data.Offset + i], Is.EqualTo(i + 10));
            }
        }
    }
}
