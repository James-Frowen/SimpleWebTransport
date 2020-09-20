using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Mirror.SimpleWeb.Tests
{

    [Category("SimpleWebTransport")]
    public class ConnectAndCloseTest : SimpleWebTestBase
    {
        [UnityTest]
        public IEnumerator ServerAcceptsConnection()
        {
            SimpleWebTransport transport = CreateRelayTransport();
            transport.ServerStart();
            int onConnectedCalled = 0;
            transport.OnServerConnected.AddListener((int connId) =>
            {
                onConnectedCalled++;
                Assert.That(connId, Is.EqualTo(1), "First connection should have id 1");
            });

            Task<RunNode.Result> task = RunNode.RunAsync("Connect.js");
            while (!task.IsCompleted)
            {
                yield return null;
            }
            RunNode.Result result = task.Result;

            Assert.That(result.timedOut, Is.False, "js should close before timeout");
            Assert.That(result.output, Has.Length.EqualTo(1), "Should have 1 log");
            Assert.That(result.output[0], Is.EqualTo("Connection opened"), "Should be connection open log");
            Assert.That(result.error, Has.Length.EqualTo(0), "Should have no errors");

            // wait for message to be processed
            yield return new WaitForSeconds(0.2f);

            Assert.That(onConnectedCalled, Is.EqualTo(1), "Connect should be called once");
        }

        [UnityTest]
        public IEnumerator ServerReactsToClose()
        {
            SimpleWebTransport transport = CreateRelayTransport();
            transport.ServerStart();
            int onConnectedCalled = 0;
            int onDisconnectedCalled = 0;
            transport.OnServerConnected.AddListener((int connId) =>
            {
                onConnectedCalled++;
                Assert.That(connId, Is.EqualTo(1), "First connection should have id 1");
            });
            transport.OnServerDisconnected.AddListener((int connId) =>
            {
                onDisconnectedCalled++;
                Assert.That(connId, Is.EqualTo(1), "First connection should have id 1");
            });

            Task<RunNode.Result> task = RunNode.RunAsync("Connect.js");
            while (!task.IsCompleted)
            {
                yield return null;
            }
            RunNode.Result result = task.Result;

            Assert.That(result.timedOut, Is.False, "js should close before timeout");
            Assert.That(result.output, Has.Length.EqualTo(1), "Should have 1 log");
            Assert.That(result.output[0], Is.EqualTo("Connection opened"), "Should be connection open log");
            Assert.That(result.error, Has.Length.EqualTo(0), "Should have no errors");

            // wait for message to be processed
            yield return new WaitForSeconds(0.2f);

            Assert.That(onConnectedCalled, Is.EqualTo(1), "Connect should be called once");
            Assert.That(onDisconnectedCalled, Is.EqualTo(1), "Disconnected should be called once");
        }

        [UnityTest]
        public IEnumerator ServerReactsWithClientAppStops()
        {
            SimpleWebTransport transport = CreateRelayTransport();
            transport.ServerStart();
            int onConnectedCalled = 0;
            int onDisconnectedCalled = 0;
            transport.OnServerConnected.AddListener((int connId) =>
            {
                onConnectedCalled++;
                Assert.That(connId, Is.EqualTo(1), "First connection should have id 1");
            });
            transport.OnServerDisconnected.AddListener((int connId) =>
            {
                onDisconnectedCalled++;
                Assert.That(connId, Is.EqualTo(1), "First connection should have id 1");
            });

            // kill js early so it doesn't send close message
            Task<RunNode.Result> task = RunNode.RunAsync("Connect.js", 1000);
            while (!task.IsCompleted)
            {
                yield return null;
            }
            RunNode.Result result = task.Result;

            Assert.That(result.timedOut, Is.True, "Should timeout app stopping before close");
            Assert.That(result.output, Has.Length.EqualTo(1), "Should have 1 log");
            Assert.That(result.output[0], Is.EqualTo("Connection opened"), "Should be connection open log");
            Assert.That(result.error, Has.Length.EqualTo(0), "Should have no errors");

            // wait for message to be processed
            yield return new WaitForSeconds(0.2f);

            Assert.That(onConnectedCalled, Is.EqualTo(1), "Connect should be called once");
            Assert.That(onDisconnectedCalled, Is.EqualTo(1), "Disconnected should be called once");
        }
    }
}
