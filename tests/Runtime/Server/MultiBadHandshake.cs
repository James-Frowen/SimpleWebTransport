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
            const int goodClientCount = 10;
            for (int i = 0; i < goodClientCount; i++)
            {
                // alternate between good and bad clients
                Task<TcpClient> createTask = CreateBadClient();
                while (!createTask.IsCompleted) { yield return null; }
                TcpClient client = createTask.Result;
                Assert.That(client.Connected, Is.True, "Client should have connected");
                badClients.Add(client);
            }
            Task<RunNode.Result> task = RunNode.RunAsync("ConnectAndClose.js", arg0: goodClientCount.ToString());

            // wait for timeout so bad clients disconnect
            yield return new WaitForSeconds(timeout / 1000);
            // wait extra second for stuff to process
            yield return new WaitForSeconds(2);

            Assert.That(onConnect, Has.Count.EqualTo(goodClientCount), "Connect should not be called");
            Assert.That(onDisconnect, Has.Count.EqualTo(goodClientCount), "Disconnect should not be called");
            Assert.That(onData, Has.Count.EqualTo(0), "Data should not be called");

            Assert.That(task.IsCompleted, Is.True, "Take should have been completed");
            RunNode.Result result = task.Result;

            result.AssetTimeout(false);
            result.AssetErrors();
            List<string> expected = new List<string>();
            for (int i = 0; i < goodClientCount; i++)
            {
                expected.Add($"{i}: Connection opened");
                expected.Add($"{i}: Closed after 2000ms");
            }
            result.AssetOutputUnordered(expected.ToArray());
        }
    }
}
