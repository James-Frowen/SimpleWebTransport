using System.Collections;
using System.Collections.Generic;
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

        List<Task<RunNode.Result>> clients = new List<Task<RunNode.Result>>();

        [UnityTest]
        public IEnumerator ConnectWithMultipleClients()
        {
            int connectIndex = 1;
            transport.OnServerConnected.AddListener((connId) =>
            {
                Assert.That(connId == connectIndex, "Clients should be connected in order with the next index");
                connectIndex++;
            });
            const int count = 100;
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
    }
}
