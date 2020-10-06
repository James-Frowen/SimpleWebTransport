using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Mirror.SimpleWeb.Tests.Client
{
    [Category("SimpleWebTransport")]
    public class ClientTests : SimpleWebClientTestBase
    {
        protected override bool StartServer => true;

        [UnityTest]
        public IEnumerator CanConnectAndDisconnectFromServer()
        {
            transport.ClientConnect("localhost");

            // wait for connect
            yield return new WaitForSeconds(1);

            Assert.That(onConnect, Is.EqualTo(1), "Connect should be called once");
            Assert.That(server_onConnect, Has.Count.EqualTo(1), "server Connect should be called once");

            transport.ClientDisconnect();

            // wait for disconnect
            yield return new WaitForSeconds(1);

            Assert.That(onDisconnect, Is.EqualTo(1), "Disconnect should be called once");
            Assert.That(server_onDisconnect, Has.Count.EqualTo(1), "server Disconnect should be called once");
        }

        [UnityTest]
        public IEnumerator CanConnectAndBeKickedFromServer()
        {
            transport.ClientConnect("localhost");

            // wait for connect
            yield return new WaitForSeconds(1);

            Assert.That(onConnect, Is.EqualTo(1), "Connect should be called once");
            Assert.That(server_onConnect, Has.Count.EqualTo(1), "server Connect should be called once");

            server.ServerDisconnect(server_onConnect[0]);

            // wait for disconnect
            yield return new WaitForSeconds(1);

            Assert.That(onDisconnect, Is.EqualTo(1), "Disconnect should be called once");
            Assert.That(server_onDisconnect, Has.Count.EqualTo(1), "server Disconnect should be called once");
        }

        [UnityTest]
        public IEnumerator CanRecieveMessage()
        {
            transport.ClientConnect("localhost");
            // wait for connect
            yield return new WaitForSeconds(1);

            byte[] bytes = Enumerable.Range(10, 10).Select(x => (byte)x).ToArray();
            ArraySegment<byte> segment = new ArraySegment<byte>(bytes, 0, 10);
            server.ServerSend(new List<int> { 1 }, 0, segment);

            // wait for message
            yield return new WaitForSeconds(1);

            Assert.That(onData, Has.Count.EqualTo(1), "should have 1 message");
            CollectionAssert.AreEqual(onData[0], bytes, "data should match");
        }

        [UnityTest]
        public IEnumerator CanSendMessage()
        {
            transport.ClientConnect("localhost");
            // wait for connect
            yield return new WaitForSeconds(1);

            byte[] bytes = Enumerable.Range(10, 10).Select(x => (byte)x).ToArray();
            ArraySegment<byte> segment = new ArraySegment<byte>(bytes, 0, 10);
            transport.ClientSend(0, segment);

            // wait for message
            yield return new WaitForSeconds(0.25f);

            Assert.That(server_onData, Has.Count.EqualTo(1), "should have 1 message");
            Assert.That(server_onData[0].connId, Is.EqualTo(1), "should be connection id");
            CollectionAssert.AreEqual(bytes, server_onData[0].data, "data should match");
        }

        [UnityTest]
        public IEnumerator CanRecieveMulitpleMessage()
        {
            transport.ClientConnect("localhost");
            // wait for connect
            yield return new WaitForSeconds(1);

            List<byte[]> messages = createRandomMessages();
            foreach (byte[] msg in messages)
            {
                ArraySegment<byte> segment = new ArraySegment<byte>(msg);
                server.ServerSend(new List<int> { 1 }, 0, segment);
                yield return new WaitForSeconds(0.05f);
            }

            // wait for message
            yield return new WaitForSeconds(1);

            Assert.That(onData, Has.Count.EqualTo(messages.Count), "should have 1 message");
            for (int i = 0; i < messages.Count; i++)
            {
                CollectionAssert.AreEqual(messages[i], onData[i], "data should match");
            }
        }
        List<byte[]> createRandomMessages(int count = 20)
        {
            List<byte[]> messages = new List<byte[]>();
            for (int i = 0; i < count; i++)
            {
                byte[] bytes = Enumerable.Range(UnityEngine.Random.Range(0, 2), UnityEngine.Random.Range(2, 5)).Select(x => (byte)x).ToArray();
                messages.Add(bytes);
            }
            return messages;
        }


        [UnityTest]
        public IEnumerator CanSendMulitpleMessage()
        {
            transport.ClientConnect("localhost");
            // wait for connect
            yield return new WaitForSeconds(1);

            List<byte[]> messages = createRandomMessages();
            foreach (byte[] msg in messages)
            {
                ArraySegment<byte> segment = new ArraySegment<byte>(msg);
                transport.ClientSend(0, segment);
                yield return new WaitForSeconds(0.05f);
            }
            // wait for message
            yield return new WaitForSeconds(0.25f);

            Assert.That(server_onData, Has.Count.EqualTo(messages.Count), "should have 1 message");
            Assert.That(server_onData[0].connId, Is.EqualTo(1), "should be connection id");
            for (int i = 0; i < messages.Count; i++)
            {
                CollectionAssert.AreEqual(messages[i], server_onData[i].data, "data should match");
            }
        }


        [UnityTest]
        public IEnumerator CanPingAndStayConnectedForTime()
        {
            transport.ClientConnect("localhost");

            // wait for connect
            yield return new WaitForSeconds(1);

            Assert.That(onConnect, Is.EqualTo(1), "Connect should be called once");
            Assert.That(server_onConnect, Has.Count.EqualTo(1), "server Connect should be called once");

            transport.ClientDisconnect();

            // wait for disconnect
            yield return new WaitForSeconds(1);

            Assert.That(onDisconnect, Is.EqualTo(1), "Disconnect should be called once");
            Assert.That(server_onDisconnect, Has.Count.EqualTo(1), "server Disconnect should be called once");
        }

    }
}
