using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Mirror.SimpleWeb.Tests.Server
{
    [Category("SimpleWebTransport")]
    public class DisconnectTest : SimpleWebTestBase
    {
        SimpleWebTransport transport;
        Task<RunNode.Result> task;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            transport = CreateRelayTransport();
            transport.ServerStart();

            task = RunNode.RunAsync("Disconnect.js");

            int onConnectedCalled = 0;
            transport.OnServerConnected.AddListener((int connId) =>
            {
                onConnectedCalled++;
            });

            yield return new WaitUntil(() => onConnectedCalled >= 1);
        }

        [UnityTest]
        public IEnumerator SendFullArray()
        {
            transport.ServerDisconnect(1);

            yield return new WaitUntil(() => task.IsCompleted);

            RunNode.Result result = task.Result;

            Assert.That(result.timedOut, Is.False, "js should close before timeout");
            Assert.That(result.output, Has.Length.EqualTo(1), "Should have 1 log");
            Assert.That(result.output[0], Is.EqualTo("Connection closed"), "Should have disconnect log");
            Assert.That(result.error, Has.Length.EqualTo(0), "Should have no errors");
        }
    }
}
