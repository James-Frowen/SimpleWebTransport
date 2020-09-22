using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Mirror.SimpleWeb.Tests.Server
{
    [Category("SimpleWebTransport")]
    public class ConnectAndCloseTest : SimpleWebTestBase
    {
        protected override bool StartServer => true;

        [UnityTest]
        public IEnumerator AcceptsConnection()
        {
            Task<RunNode.Result> task = RunNode.RunAsync("ConnectAndClose.js");
            while (!task.IsCompleted)
            {
                yield return null;
            }
            RunNode.Result result = task.Result;

            Assert.That(result.timedOut, Is.False, "js should close before timeout");
            Assert.That(result.output, Has.Length.EqualTo(2), "Should have 2 log");
            Assert.That(result.output[0], Is.EqualTo("Connection opened"), "Should be connection open log");
            Assert.That(result.output[1], Is.EqualTo($"Closed after 2000ms"), "Should be connection close log");
            Assert.That(result.error, Has.Length.EqualTo(0), "Should have no errors");

            // wait for message to be processed
            yield return new WaitForSeconds(0.2f);

            Assert.That(onConnectedCount, Is.EqualTo(1), "Connect should be called once");
        }

        [UnityTest]
        public IEnumerator ReactsToClose()
        {
            Task<RunNode.Result> task = RunNode.RunAsync("ConnectAndClose.js");
            while (!task.IsCompleted)
            {
                yield return null;
            }
            RunNode.Result result = task.Result;

            Assert.That(result.timedOut, Is.False, "js should close before timeout");
            Assert.That(result.output, Has.Length.EqualTo(2), "Should have 2 log");
            Assert.That(result.output[0], Is.EqualTo("Connection opened"), "Should be connection open log");
            Assert.That(result.output[1], Is.EqualTo($"Closed after 2000ms"), "Should be connection close log");
            Assert.That(result.error, Has.Length.EqualTo(0), "Should have no errors");

            // wait for message to be processed
            yield return new WaitForSeconds(0.2f);

            Assert.That(onConnectedCount, Is.EqualTo(1), "Connect should be called once");
            Assert.That(onDisconnectedCount, Is.EqualTo(1), "Disconnected should be called once");
        }

        [UnityTest]
        public IEnumerator ShouldTimeoutClientAfterClientProcessIsKilled()
        {
            // kill js early so it doesn't send close message
            Task<RunNode.Result> task = RunNode.RunAsync("Connect.js", 1000);
            while (!task.IsCompleted)
            {
                yield return null;
            }
            RunNode.Result result = task.Result;

            Assert.That(result.timedOut, Is.True, "Should have timed out");
            Assert.That(result.output, Has.Length.EqualTo(1), "Should have 1 log, should not have close log as process was killed");
            Assert.That(result.output[0], Is.EqualTo("Connection opened"), "Should be connection open log");
            Assert.That(result.error, Has.Length.EqualTo(0), "Should have no errors");

            // wait for timeout
            yield return new WaitForSeconds(timeout / 1000);
            // give time to process message
            yield return new WaitForSeconds(1);

            Assert.That(onConnectedCount, Is.EqualTo(1), "Connect should be called once");
            Assert.That(onDisconnectedCount, Is.EqualTo(1), "Disconnected should be called once");
        }

        [UnityTest]
        public IEnumerator ShouldTimeoutClientAfterNoMessage()
        {
            // make sure doesn't timeout
            Task<RunNode.Result> task = RunNode.RunAsync("Connect.js", timeout * 2);

            // wait for timeout
            yield return new WaitForSeconds(timeout / 1000);
            // give time to process message
            yield return new WaitForSeconds(1);

            Assert.That(onConnectedCount, Is.EqualTo(1), "Connect should be called once");
            Assert.That(onDisconnectedCount, Is.EqualTo(1), "Disconnected should be called once");

            yield return new WaitForSeconds(0.2f);

            Assert.That(task.IsCompleted, Is.True, "Connect.js should have stopped after connection was closed by timeout");
            RunNode.Result result = task.Result;

            Assert.That(result.timedOut, Is.False, "js should close before timeout");
            Assert.That(result.output, Has.Length.EqualTo(2), "Should have 2 log");
            Assert.That(result.output[0], Is.EqualTo("Connection opened"), "Should be connection open log");
            Assert.That(result.output[1], Is.EqualTo($"Connection closed"), "Should be connection close log");
            Assert.That(result.error, Has.Length.EqualTo(0), "Should have no errors");
        }
    }
}
