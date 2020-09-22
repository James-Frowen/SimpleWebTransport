using System.Collections;
using System.Net.Sockets;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Mirror.SimpleWeb.Tests.Server
{
    [Category("SimpleWebTransport")]
    public class BadHandshake : BadClientTestBase
    {
        TcpClient client;

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();

            client?.Dispose();
        }


        [UnityTest]
        public IEnumerator ClosesConnectionIfBadData()
        {
            Task<TcpClient> createTask = CreateBadClient();
            while (!createTask.IsCompleted) { yield return null; }
            client = createTask.Result;
            Assert.That(client.Connected, Is.True, "Client should have connected");

            WriteBadData(client);

            // wait for message to be processed
            yield return new WaitForSeconds(1f);

            Assert.That(HasDisconnected(client), Is.True, "Client should have been disconnected");

            Assert.That(onConnectedCalled, Is.EqualTo(0), "Connect should not be called");
            Assert.That(onDisconnectedCalled, Is.EqualTo(0), "Disconnect should not be called");
            Assert.That(onDataReceived, Is.EqualTo(0), "Data should not be called");
        }


        [UnityTest]
        public IEnumerator ClosesConnectionIfNoHandShakeInTimeout()
        {
            Task<TcpClient> createTask = CreateBadClient();
            while (!createTask.IsCompleted) { yield return null; }
            client = createTask.Result;
            Assert.That(client.Connected, Is.True, "Client should have connected");

            // wait for timeout
            yield return new WaitForSeconds(timeout / 1000);
            // wait for time to process timeout
            yield return new WaitForSeconds(1);

            Assert.That(HasDisconnected(client), Is.True, "Client should have been disconnected");

            Assert.That(onConnectedCalled, Is.EqualTo(0), "Connect should not be called");
            Assert.That(onDisconnectedCalled, Is.EqualTo(0), "Disconnect should not be called");
            Assert.That(onDataReceived, Is.EqualTo(0), "Data should not be called");
        }

        [UnityTest]
        public IEnumerator OtherClientsCanConnectWhileWaitingForBadClient()
        {
            // connect bad client
            Task<TcpClient> createTask = CreateBadClient();
            while (!createTask.IsCompleted) { yield return null; }
            client = createTask.Result;
            Assert.That(client.Connected, Is.True, "Client should have connected");

            // connect good client
            Task<RunNode.Result> task = RunNode.RunAsync("ConnectAndClose.js");
            while (!task.IsCompleted)
            {
                yield return null;
            }

            // check good client connected and then closed by itself
            RunNode.Result result = task.Result;
            Assert.That(result.timedOut, Is.False, "js should close before timeout");
            Assert.That(result.output, Has.Length.EqualTo(2), "Should have 2 log");
            Assert.That(result.output[0], Is.EqualTo("Connection opened"), "Should be connection open log");
            Assert.That(result.output[1], Is.EqualTo($"Closed after 2000ms"), "Should be connection close log");
            Assert.That(result.error, Has.Length.EqualTo(0), "Should have no errors");

            // check server events
            Assert.That(onConnectedCalled, Is.EqualTo(1), "Connect should have been called once");
            Assert.That(onDisconnectedCalled, Is.EqualTo(1), "Disconnect should have been called once");
            Assert.That(onDataReceived, Is.EqualTo(0), "Data should not be called");


            // wait for timeout
            yield return new WaitForSeconds(timeout / 1000);

            Assert.That(HasDisconnected(client), Is.True, "Client should have been disconnected");
        }

    }
}
