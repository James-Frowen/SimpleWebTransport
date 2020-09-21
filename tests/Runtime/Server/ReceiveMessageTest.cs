using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Mirror.SimpleWeb.Tests.Server
{
    [Category("SimpleWebTransport")]
    public class ReceiveMessageTest : SimpleWebTestBase
    {
        SimpleWebTransport transport;
        List<(int connId, ArraySegment<byte> data)> messages = new List<(int connId, ArraySegment<byte> data)>();

        [UnitySetUp]
        public IEnumerator Setup()
        {
            messages.Clear();

            transport = CreateRelayTransport();
            transport.ServerStart();


            int onConnectedCalled = 0;
            transport.OnServerConnected.AddListener((connId) =>
            {
                onConnectedCalled++;
            });
            transport.OnServerDataReceived.AddListener((connId, data, channel) =>
            {
                messages.Add((connId, data));
            });

            // dont worry about result, run will timeout by itself
            _ = RunNode.RunAsync("SendMessages.js");

            yield return new WaitUntil(() => onConnectedCalled >= 1);
        }

        [UnityTest]
        public IEnumerator SendFullArray()
        {
            // wait for message
            yield return new WaitForSeconds(0.5f);

            Assert.That(messages, Has.Count.EqualTo(1), "Should have 1 message");
            Assert.That(messages[0].connId, Is.EqualTo(1), "Connd id should be 1");
            ArraySegment<byte> data = messages[0].data;
            Assert.That(data.Count, Is.EqualTo(10), "Shoudl have 10 bytes");
            for (int i = 0; i < 10; i++)
            {
                // js sends i+10 for each byte
                Assert.That(data.Array[data.Offset + i], Is.EqualTo(i + 10));
            }
        }
    }
}
