using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Mirror.SimpleWeb.Tests
{
    [Category("SimpleWebTransport")]
    public class StartAndStopTest : SimpleWebTestBase
    {
        [UnityTest]
        public IEnumerator ServerCanStartAndStopWithoutErrors()
        {
            SimpleWebTransport transport = CreateRelayTransport();
            transport.ServerStart();

            yield return new WaitForSeconds(0.2f);

            transport.ServerStop();

            yield return new WaitForSeconds(0.2f);

            Assert.Pass("Pass becuase there is no errors");
        }
    }

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

            Assert.That(result.timedOut, Is.False);
            Assert.That(result.output, Has.Length.EqualTo(1));
            Assert.That(result.output[0], Is.EqualTo("Connection opened"));
            Assert.That(result.error, Has.Length.EqualTo(0));

            // wait for message to be processed
            yield return new WaitForSeconds(0.2f);

            Assert.That(onConnectedCalled, Is.EqualTo(1));
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

            Assert.That(result.timedOut, Is.False);
            Assert.That(result.output, Has.Length.EqualTo(1));
            Assert.That(result.output[0], Is.EqualTo("Connection opened"));
            Assert.That(result.error, Has.Length.EqualTo(0));

            // wait for message to be processed
            yield return new WaitForSeconds(0.2f);

            Assert.That(onConnectedCalled, Is.EqualTo(1));
            Assert.That(onDisconnectedCalled, Is.EqualTo(1));
        }
    }
}
