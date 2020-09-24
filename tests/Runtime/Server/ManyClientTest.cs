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
            List<Task<RunNode.Result>> clients = new List<Task<RunNode.Result>>();

            int connectIndex = 1;
            transport.OnServerConnected.AddListener((connId) =>
            {
                Assert.That(connId == connectIndex, "Clients should be connected in order with the next index");
                connectIndex++;
            });

            for (int i = 0; i < count; i++)
            {
                // connect good client
                Task<RunNode.Result> task = RunNode.RunAsync("ConnectAndClose.js");
                clients.Add(task);

                yield return null;
            }

            // 4 seconds should be enough time for clients to connect then close themselves
            yield return new WaitForSeconds(4);

            Assert.That(onConnect, Has.Count.EqualTo(count), "All should be connectted");
            Assert.That(onDisconnect, Has.Count.EqualTo(count), "All should be disconnected called");
            Assert.That(onData, Has.Count.EqualTo(0), "Data should not be called");

            for (int i = 0; i < count; i++)
            {
                Task<RunNode.Result> task = clients[0];
                Assert.That(task.IsCompleted, Is.True, "Take should have been completed");

                RunNode.Result result = task.Result;

                Assert.That(result.timedOut, Is.False, "js should close before timeout");
                Assert.That(result.output, Has.Length.EqualTo(2), "Should have 2 log");
                Assert.That(result.output[0], Is.EqualTo("Connection opened"), "Should be connection open log");
                Assert.That(result.output[1], Is.EqualTo($"Closed after 2000ms"), "Should be connection close log");
                Assert.That(result.error, Has.Length.EqualTo(0), "Should have no errors");
            }
        }

        [UnityTest]
        [TestCase(1, ExpectedResult = null)]
        [TestCase(10, ExpectedResult = null)]
        [TestCase(100, ExpectedResult = null)]
        public IEnumerator ManyPings(int count)
        {
            List<Task<RunNode.Result>> clients = new List<Task<RunNode.Result>>();

            int connectIndex = 1;
            transport.OnServerConnected.AddListener((connId) =>
            {
                Assert.That(connId == connectIndex, "Clients should be connected in order with the next index");
                connectIndex++;
            });

            for (int i = 0; i < count; i++)
            {
                // connect good client
                Task<RunNode.Result> task = RunNode.RunAsync("Ping.js", 30_000);
                clients.Add(task);

                yield return null;
            }

            // 4 seconds should be enough time for clients to connect then close themselves
            yield return new WaitForSeconds(4);

            Assert.That(onConnect, Has.Count.EqualTo(count), "All should be connectted");


            // make sure all clients start connected for a while
            const int seconds = 10;
            for (float i = 0; i < seconds; i += Time.deltaTime)
            {
                Assert.That(onDisconnect, Has.Count.EqualTo(0), "no clients should be disconnected");

                yield return null;
            }

            for (int i = 0; i < count; i++)
            {
                // 1 indexed
                transport.ServerDisconnect(i + 1);
            }

            // wait for all to disconnect
            yield return new WaitForSeconds(1);
            Assert.That(onDisconnect, Has.Count.EqualTo(count), "no clients should be disconnected");

            // check all tasks finished with no logs
            for (int i = 0; i < count; i++)
            {
                Task<RunNode.Result> task = clients[0];
                Assert.That(task.IsCompleted, Is.True, "Take should have been completed");

                RunNode.Result result = task.Result;

                Assert.That(result.timedOut, Is.False, "js should close before timeout");
                Assert.That(result.output, Has.Length.EqualTo(0), "Should have 2 log");
                Assert.That(result.error, Has.Length.EqualTo(0), "Should have no errors");
            }


            List<ArraySegment<byte>>[] messageForClients = Enumerable.Repeat(0, count).Select(x => new List<ArraySegment<byte>>()).ToArray();
            onData.ForEach(x => messageForClients[x.connId - 1 /*from 1 index to 0 indexed*/].Add(x.data));
            for (int i = 0; i < count; i++)
            {
                List<ArraySegment<byte>> messages = messageForClients[i];
                int expected = seconds - 1;
                Assert.That(messages, Has.Count.AtLeast(expected), $"Should have atleast {expected} ping messages");

                foreach (ArraySegment<byte> message in messages)
                {
                    Assert.That(message.Array[message.Offset + 0], Is.EqualTo(10), "first byte should be 10");
                    Assert.That(message.Array[message.Offset + 1], Is.EqualTo(11), "second byte should be 11");
                    Assert.That(message.Array[message.Offset + 2], Is.EqualTo(12), "thrid byte should be 12");
                    Assert.That(message.Array[message.Offset + 3], Is.EqualTo(13), "fourth byte should be 13");
                }
            }
        }


        [UnityTest]
        [TestCase(1, ExpectedResult = null)]
        [TestCase(10, ExpectedResult = null)]
        [TestCase(100, ExpectedResult = null)]
        public IEnumerator ManySend(int count)
        {
            List<Task<RunNode.Result>> clients = new List<Task<RunNode.Result>>();

            int connectIndex = 1;
            transport.OnServerConnected.AddListener((connId) =>
            {
                Assert.That(connId, Is.EqualTo(connectIndex), "Clients should be connected in order with the next index");
                connectIndex++;
            });

            for (int i = 0; i < count; i++)
            {
                // connect good client
                Task<RunNode.Result> task = RunNode.RunAsync("Ping.js", 30_000);
                clients.Add(task);

                yield return null;
            }

            // 4 seconds should be enough time for clients to connect then close themselves
            yield return new WaitForSeconds(4);

            Assert.That(onConnect, Has.Count.EqualTo(count), "All should be connectted");


            // make sure all clients start connected for a while
            const int seconds = 10;
            const float sendInterval = 0.1f;
            float nextSend = 0;
            List<int> allIds = Enumerable.Range(1, count).ToList();
            ArraySegment<byte> segment = CreateMessage();

            for (float i = 0; i < seconds; i += Time.deltaTime)
            {
                Assert.That(onDisconnect, Has.Count.EqualTo(0), "no clients should be disconnected");

                if (nextSend < i)
                {
                    //send
                    transport.ServerSend(allIds, Channels.DefaultReliable, segment);

                    nextSend += sendInterval;
                }

                yield return null;
            }

            for (int i = 0; i < count; i++)
            {
                // 1 indexed
                transport.ServerDisconnect(i + 1);
            }

            // wait for all to disconnect
            yield return new WaitForSeconds(1);
            Assert.That(onDisconnect, Has.Count.EqualTo(count), "no clients should be disconnected");

            // check all tasks finished with no logs
            for (int i = 0; i < count; i++)
            {
                Task<RunNode.Result> task = clients[0];
                Assert.That(task.IsCompleted, Is.True, "Take should have been completed");

                RunNode.Result result = task.Result;

                Assert.That(result.timedOut, Is.False, "js should close before timeout");
                Assert.That(result.output, Has.Length.EqualTo(0), "Should have 2 log");
                Assert.That(result.error, Has.Length.EqualTo(0), "Should have no errors");
            }


            List<ArraySegment<byte>>[] messageForClients = Enumerable.Repeat(0, count).Select(x => new List<ArraySegment<byte>>()).ToArray();
            onData.ForEach(x => messageForClients[x.connId - 1 /*from 1 index to 0 indexed*/].Add(x.data));
            for (int i = 0; i < count; i++)
            {
                List<ArraySegment<byte>> messages = messageForClients[i];
                int expected = seconds - 1;
                Assert.That(messages, Has.Count.AtLeast(expected), $"Should have atleast {expected} ping messages");

                foreach (ArraySegment<byte> message in messages)
                {
                    Assert.That(message.Array[message.Offset + 0], Is.EqualTo(10), "first byte should be 10");
                    Assert.That(message.Array[message.Offset + 1], Is.EqualTo(11), "second byte should be 11");
                    Assert.That(message.Array[message.Offset + 2], Is.EqualTo(12), "thrid byte should be 12");
                    Assert.That(message.Array[message.Offset + 3], Is.EqualTo(13), "fourth byte should be 13");
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
