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
    public class ClientTests : SimpleWebTestBase
    {
        protected override bool StartServer => true;

        [UnityTest]
        public IEnumerator CanConnectAndDisconnectFromServer()
        {
            client.ClientConnect("localhost");

            // wait for connect
            yield return new WaitForSeconds(1);

            Assert.That(client.onConnect, Is.EqualTo(1), "Connect should be called once");
            Assert.That(server.onConnect, Has.Count.EqualTo(1), "server Connect should be called once");

            client.ClientDisconnect();

            // wait for disconnect
            yield return new WaitForSeconds(1);

            Assert.That(client.onDisconnect, Is.EqualTo(1), "Disconnect should be called once");
            Assert.That(server.onDisconnect, Has.Count.EqualTo(1), "server Disconnect should be called once");
        }

        [UnityTest]
        public IEnumerator CanConnectAndBeKickedFromServer()
        {
            client.ClientConnect("localhost");

            // wait for connect
            yield return new WaitForSeconds(1);

            Assert.That(client.onConnect, Is.EqualTo(1), "Connect should be called once");
            Assert.That(server.onConnect, Has.Count.EqualTo(1), "server Connect should be called once");

            server.ServerDisconnect(server.onConnect[0]);

            // wait for disconnect
            yield return new WaitForSeconds(1);

            Assert.That(client.onDisconnect, Is.EqualTo(1), "Disconnect should be called once");
            Assert.That(server.onDisconnect, Has.Count.EqualTo(1), "server Disconnect should be called once");
        }

        [UnityTest]
        public IEnumerator CanRecieveMessage()
        {
            client.ClientConnect("localhost");
            // wait for connect
            yield return new WaitForSeconds(1);

            byte[] bytes = Enumerable.Range(10, 10).Select(x => (byte)x).ToArray();
            ArraySegment<byte> segment = new ArraySegment<byte>(bytes);
            server.ServerSend(new List<int> { 1 }, 0, segment);

            // wait for message
            yield return new WaitForSeconds(1);

            Assert.That(client.onData, Has.Count.EqualTo(1), "should have 1 message");
            CollectionAssert.AreEqual(client.onData[0], bytes, "data should match");
        }

        [UnityTest]
        public IEnumerator CanSendMessage()
        {
            client.ClientConnect("localhost");
            // wait for connect
            yield return new WaitForSeconds(1);

            byte[] bytes = Enumerable.Range(10, 10).Select(x => (byte)x).ToArray();
            ArraySegment<byte> segment = new ArraySegment<byte>(bytes);
            client.ClientSend(0, segment);

            // wait for message
            yield return new WaitForSeconds(0.25f);

            Assert.That(server.onData, Has.Count.EqualTo(1), "should have 1 message");
            Assert.That(server.onData[0].connId, Is.EqualTo(1), "should be connection id");
            CollectionAssert.AreEqual(bytes, server.onData[0].data, "data should match");
        }

        [UnityTest]
        public IEnumerator CanRecieveMulitpleMessages()
        {
            client.ClientConnect("localhost");
            // wait for connect
            yield return client.WaitForConnect;

            List<byte[]> messages = createRandomMessages();
            foreach (byte[] msg in messages)
            {
                ArraySegment<byte> segment = new ArraySegment<byte>(msg);
                server.ServerSend(new List<int> { 1 }, 0, segment);
                yield return null;
            }

            // wait for message
            yield return new WaitForSeconds(1f);


            Assert.That(client.onData, Has.Count.EqualTo(messages.Count), $"should have {messages.Count} message");
            for (int i = 0; i < messages.Count; i++)
            {
                CollectionAssert.AreEqual(messages[i], client.onData[i], $"data[{i}] should match");
            }
        }

        [UnityTest]
        public IEnumerator CanSendMulitpleMessages()
        {
            client.ClientConnect("localhost");
            // wait for connect
            yield return new WaitForSeconds(1);

            List<byte[]> messages = createRandomMessages();
            foreach (byte[] msg in messages)
            {
                ArraySegment<byte> segment = new ArraySegment<byte>(msg);
                client.ClientSend(0, segment);
                yield return null;
            }

            // wait for message
            yield return new WaitForSeconds(1f);

            Assert.That(server.onData, Has.Count.EqualTo(messages.Count), $"should have {messages.Count} message");
            Assert.That(server.onData[0].connId, Is.EqualTo(1), "should be connection id");
            for (int i = 0; i < messages.Count; i++)
            {
                CollectionAssert.AreEqual(messages[i], server.onData[i].data, $"data[{i}] should match");
            }
        }

        List<byte[]> createRandomMessages(int count = 20)
        {
            List<byte[]> messages = new List<byte[]>();
            for (int i = 0; i < count; i++)
            {
                int start = UnityEngine.Random.Range(0, 255);
                int length = UnityEngine.Random.Range(2, 50);
                byte[] bytes = Enumerable.Range(start, length).Select(x => (byte)x).ToArray();
                messages.Add(bytes);
            }
            return messages;
        }


        [UnityTest]
        [TestCase(1, ExpectedResult = default)]
        [TestCase(2, ExpectedResult = default)]
        [TestCase(3, ExpectedResult = default)]
        [TestCase(4, ExpectedResult = default)]
        public IEnumerator CanRecieveShortMessage(int messageSize)
        {
            client.ClientConnect("localhost");
            // wait for connect
            yield return new WaitForSeconds(1);

            byte[] bytes = new byte[messageSize];
            new System.Random().NextBytes(bytes);
            ArraySegment<byte> segment = new ArraySegment<byte>(bytes);
            server.ServerSend(new List<int> { 1 }, 0, segment);

            // wait for message
            yield return new WaitForSeconds(1);

            Assert.That(client.onData, Has.Count.EqualTo(1), "should have 1 message");
            CollectionAssert.AreEqual(client.onData[0], bytes, "data should match");
        }

        [UnityTest]
        [TestCase(1, ExpectedResult = default)]
        [TestCase(2, ExpectedResult = default)]
        [TestCase(3, ExpectedResult = default)]
        [TestCase(4, ExpectedResult = default)]
        public IEnumerator CanSendShortMessage(int messageSize)
        {
            client.ClientConnect("localhost");
            // wait for connect
            yield return new WaitForSeconds(1);

            byte[] bytes = new byte[messageSize];
            new System.Random().NextBytes(bytes);
            ArraySegment<byte> segment = new ArraySegment<byte>(bytes);
            client.ClientSend(0, segment);

            // wait for message
            yield return new WaitForSeconds(1);

            Assert.That(server.onData, Has.Count.EqualTo(1), "should have 1 message");
            Assert.That(server.onData[0].connId, Is.EqualTo(1), "should be connection id");
            CollectionAssert.AreEqual(bytes, server.onData[0].data, "data should match");
        }

        [UnityTest]
        public IEnumerator CanPingAndStayConnectedForTime()
        {
            client.ClientConnect("localhost");

            // wait for connect
            yield return new WaitForSeconds(1);

            Assert.That(client.onConnect, Is.EqualTo(1), "Connect should be called once");
            Assert.That(server.onConnect, Has.Count.EqualTo(1), "server Connect should be called once");

            client.ClientDisconnect();

            // wait for disconnect
            yield return new WaitForSeconds(1);

            Assert.That(client.onDisconnect, Is.EqualTo(1), "Disconnect should be called once");
            Assert.That(server.onDisconnect, Has.Count.EqualTo(1), "server Disconnect should be called once");
        }

        [UnityTest]
        public IEnumerator ErrorWhenMessageTooBig()
        {
            client.ClientConnect("localhost");
            // wait for connect
            yield return new WaitForSeconds(1);

            ArraySegment<byte> segment = new ArraySegment<byte>(new byte[70_000]);

            LogAssert.Expect(LogType.Error, "Message greater than max size");
            bool result = client.ClientSend(Channels.DefaultReliable, segment);

            Assert.IsFalse(result);
        }

        [UnityTest]
        public IEnumerator ErrorWhenMessageTooSmall()
        {
            client.ClientConnect("localhost");
            // wait for connect
            yield return new WaitForSeconds(1);

            ArraySegment<byte> segment = new ArraySegment<byte>();

            LogAssert.Expect(LogType.Error, "Message count was zero");
            bool result = client.ClientSend(Channels.DefaultReliable, segment);

            Assert.IsFalse(result);
        }
    }
}
