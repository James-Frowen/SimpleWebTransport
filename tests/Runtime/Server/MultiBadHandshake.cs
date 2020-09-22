using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Mirror.SimpleWeb.Tests.Server
{
    [Category("SimpleWebTransport")]
    public class MultiBadHandshake : SimpleWebTestBase
    {
        protected override bool StartServer => true;

        List<TcpClient> badClients = new List<TcpClient>();
        List<Task<RunNode.Result>> goodClients = new List<Task<RunNode.Result>>();

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();

            foreach (TcpClient bad in badClients)
            {
                bad.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator MultipleGoodAndBadClients()
        {
            int connectIndex = 1;
            transport.OnServerConnected.AddListener((connId) =>
            {
                Assert.That(connId == connectIndex, "Clients should be connected in order with the next index");
                connectIndex++;
            });
            for (int i = 0; i < 20; i++)
            {
                // alternate between good and bad clients
                if (i % 2 == 0)
                {
                    Task<TcpClient> createTask = CreateBadClient();
                    while (!createTask.IsCompleted) { yield return null; }
                    TcpClient client = createTask.Result;
                    Assert.That(client.Connected, Is.True, "Client should have connected");
                    badClients.Add(client);
                }
                else
                {
                    // connect good client
                    Task<RunNode.Result> task = RunNode.RunAsync("ConnectAndClose.js");
                    goodClients.Add(task);
                    yield return null;
                }
            }

            // wait for timeout so bad clients disconnect
            yield return new WaitForSeconds(timeout / 1000);
            // wait extra second for stuff to process
            yield return new WaitForSeconds(1);

            Assert.That(onConnectedCalled, Is.EqualTo(10), "Connect should not be called");
            Assert.That(onDisconnectedCalled, Is.EqualTo(10), "Disconnect should not be called");
            Assert.That(onDataReceived, Is.EqualTo(0), "Data should not be called");

            for (int i = 0; i < 10; i++)
            {
                Task<RunNode.Result> task = goodClients[0];
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
