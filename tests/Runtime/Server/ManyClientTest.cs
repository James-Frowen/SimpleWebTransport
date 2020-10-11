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
    public class ManyClientTest : SimpleWebTestBase
    {
        protected override bool StartServer => true;

        [UnityTest]
        [TestCase(1, ExpectedResult = null)]
        [TestCase(10, ExpectedResult = null)]
        [TestCase(100, ExpectedResult = null)]
        public IEnumerator ManyConnect(int count)
        {
            int connectIndex = 1;
            server.OnServerConnected.AddListener((connId) =>
            {
                Assert.That(connId, Is.EqualTo(connectIndex), "Clients should be connected in order with the next index");
                connectIndex++;
            });

            Task<RunNode.Result> task = RunNode.RunAsync("ConnectAndCloseMany.js", arg0: count.ToString(), msTimeout: 10000);

            // 10 seconds should be enough time for clients to connect then close themselves
            yield return new WaitForSeconds(10);

            Assert.That(server.onConnect, Has.Count.EqualTo(count), "All should be connectted");
            Assert.That(server.onDisconnect, Has.Count.EqualTo(count), "All should be disconnected called");
            Assert.That(server.onData, Has.Count.EqualTo(0), "Data should not be called");


            Assert.That(task.IsCompleted, Is.True, "Take should have been completed");

            RunNode.Result result = task.Result;
            result.AssetTimeout(false);
            result.AssetErrors();
            List<string> expected = new List<string>();
            for (int i = 0; i < count; i++)
            {
                expected.Add($"{i}: Connection opened");
                expected.Add($"{i}: Closed after 2000ms");
            }
            result.AssetOutputUnordered(expected.ToArray());
        }

        [UnityTest]
        [TestCase(1, ExpectedResult = null)]
        [TestCase(10, ExpectedResult = null)]
        [TestCase(100, ExpectedResult = null)]
        public IEnumerator ManyPings(int count)
        {
            int connectIndex = 1;
            server.OnServerConnected.AddListener((connId) =>
            {
                Assert.That(connId == connectIndex, "Clients should be connected in order with the next index");
                connectIndex++;
            });

            Task<RunNode.Result> task = RunNode.RunAsync("Ping.js", arg0: count.ToString(), msTimeout: 30_000);

            // 4 seconds should be enough time for clients to connect then close themselves
            yield return new WaitForSeconds(4);

            Assert.That(server.onConnect, Has.Count.EqualTo(count), "All should be connectted");


            // make sure all clients start connected for a while
            const int seconds = 10;
            for (float i = 0; i < seconds; i += Time.deltaTime)
            {
                Assert.That(server.onDisconnect, Has.Count.EqualTo(0), "no clients should be disconnected");

                yield return null;
            }

            for (int i = 0; i < count; i++)
            {
                // 1 indexed
                server.ServerDisconnect(i + 1);
            }

            // wait for all to disconnect
            yield return new WaitForSeconds(1);
            Assert.That(server.onDisconnect, Has.Count.EqualTo(count), "no clients should be disconnected");

            // check all tasks finished with no logs
            Assert.That(task.IsCompleted, Is.True, "Take should have been completed");

            RunNode.Result result = task.Result;

            result.AssetTimeout(false);
            result.AssetOutput();
            result.AssetErrors();

            List<byte[]>[] messageForClients = sortMessagesForClients(count, server.onData);

            for (int i = 0; i < count; i++)
            {
                List<byte[]> messages = messageForClients[i];
                int expected = seconds - 1;
                Assert.That(messages, Has.Count.AtLeast(expected), $"Should have atleast {expected} ping messages");

                foreach (byte[] message in messages)
                {
                    Assert.That(message[0], Is.EqualTo(10), "first byte should be 10");
                    Assert.That(message[1], Is.EqualTo(11), "second byte should be 11");
                    Assert.That(message[2], Is.EqualTo(12), "thrid byte should be 12");
                    Assert.That(message[3], Is.EqualTo(13), "fourth byte should be 13");
                }
            }
        }

        private List<byte[]>[] sortMessagesForClients(int clientCount, List<(int connId, byte[] data)> onData)
        {
            List<byte[]>[] messageForClients = new List<byte[]>[clientCount];
            for (int i = 0; i < clientCount; i++)
            {
                messageForClients[i] = new List<byte[]>();
            }

            foreach ((int connId, byte[] data) in onData)
            {
                //from 1 index to 0 indexed
                List<byte[]> list = messageForClients[connId - 1];
                list.Add(data);
            }

            return messageForClients;
        }

        [UnityTest]
        [TestCase(1, ExpectedResult = null)]
        [TestCase(10, ExpectedResult = null)]
        [TestCase(100, ExpectedResult = null)]
        public IEnumerator ManySend(int count)
        {
            int connectIndex = 1;
            server.OnServerConnected.AddListener((connId) =>
            {
                Assert.That(connId, Is.EqualTo(connectIndex), "Clients should be connected in order with the next index");
                connectIndex++;
            });

            Task<RunNode.Result> task = RunNode.RunAsync("Ping.js", arg0: count.ToString(), msTimeout: 30_000);

            // 4 seconds should be enough time for clients to connect then close themselves
            yield return new WaitForSeconds(4);

            Assert.That(server.onConnect, Has.Count.EqualTo(count), "All should be connectted");


            // make sure all clients start connected for a while
            const int seconds = 10;
            const float sendInterval = 0.1f;
            float nextSend = 0;
            List<int> allIds = Enumerable.Range(1, count).ToList();
            ArraySegment<byte> segment = CreateMessage();

            for (float i = 0; i < seconds; i += Time.deltaTime)
            {
                Assert.That(server.onDisconnect, Has.Count.EqualTo(0), "no clients should be disconnected");

                if (nextSend < i)
                {
                    //send
                    server.ServerSend(allIds, Channels.DefaultReliable, segment);

                    nextSend += sendInterval;
                }

                yield return null;
            }

            for (int i = 0; i < count; i++)
            {
                // 1 indexed
                server.ServerDisconnect(i + 1);
            }

            // wait for all to disconnect
            yield return new WaitForSeconds(1);
            Assert.That(server.onDisconnect, Has.Count.EqualTo(count), "no clients should be disconnected");

            Assert.That(task.IsCompleted, Is.True, "Take should have been completed");

            RunNode.Result result = task.Result;

            result.AssetTimeout(false);
            result.AssetOutput();
            result.AssetErrors();

            List<byte[]>[] messageForClients = sortMessagesForClients(count, server.onData);
            for (int i = 0; i < count; i++)
            {
                List<byte[]> messages = messageForClients[i];
                int expected = seconds - 1;
                Assert.That(messages, Has.Count.AtLeast(expected), $"Should have atleast {expected} ping messages");

                foreach (byte[] message in messages)
                {
                    Assert.That(message[0], Is.EqualTo(10), "first byte should be 10");
                    Assert.That(message[1], Is.EqualTo(11), "second byte should be 11");
                    Assert.That(message[2], Is.EqualTo(12), "thrid byte should be 12");
                    Assert.That(message[3], Is.EqualTo(13), "fourth byte should be 13");
                }
            }
        }

        static ArraySegment<byte> CreateMessage()
        {
            NetworkWriter writer = new NetworkWriter();
            MessagePacker.Pack(new RpcMessage
            {
                componentIndex = 2,
                functionHash = typeof(RpcMessage).GetHashCode(),// any hash is fine for this test
                netId = 10,
                payload = new Func<ArraySegment<byte>>(() =>
                {
                    NetworkWriter writer2 = new NetworkWriter();
                    writer2.WriteVector3(new Vector3(1, 2, 3));
                    writer2.WriteQuaternion(Quaternion.FromToRotation(Vector3.forward, Vector3.left));
                    writer2.WriteVector3(Vector3.one);

                    return writer2.ToArraySegment();
                }).Invoke()
            }, writer);
            ArraySegment<byte> segment = writer.ToArraySegment();
            return segment;
        }
    }
}
