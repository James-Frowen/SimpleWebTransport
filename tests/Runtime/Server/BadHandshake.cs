using System.Collections;
using System.Net.Sockets;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Mirror.SimpleWeb.Tests.Server
{
    [Category("SimpleWebTransport")]
    public class BadHandshake : SimpleWebTestBase
    {
        protected override bool StartServer => true;

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

            Assert.That(onConnect, Has.Count.EqualTo(0), "Connect should not be called");
            Assert.That(onDisconnect, Has.Count.EqualTo(0), "Disconnect should not be called");
            Assert.That(onData, Has.Count.EqualTo(0), "Data should not be called");
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

            Assert.That(onConnect, Has.Count.EqualTo(0), "Connect should not be called");
            Assert.That(onDisconnect, Has.Count.EqualTo(0), "Disconnect should not be called");
            Assert.That(onData, Has.Count.EqualTo(0), "Data should not be called");
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

            result.AssetTimeout(false);
            result.AssetOutput(
                "Connection opened",
                "Closed after 2000ms"
                );
            result.AssetErrors();

            // check server events
            Assert.That(onConnect, Has.Count.EqualTo(1), "Connect should have been called once");
            Assert.That(onDisconnect, Has.Count.EqualTo(1), "Disconnect should have been called once");
            Assert.That(onData, Has.Count.EqualTo(0), "Data should not be called");


            // wait for timeout
            yield return new WaitForSeconds(timeout / 1000);

            Assert.That(HasDisconnected(client), Is.True, "Client should have been disconnected");
        }
    }
}
