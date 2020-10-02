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

            result.AssetTimeout(false);
            result.AssetOutput(
                "Connection opened",
                "Closed after 2000ms"
                );
            result.AssetErrors();

            // wait for message to be processed
            yield return new WaitForSeconds(0.2f);

            Assert.That(onConnect, Has.Count.EqualTo(1), "Connect should be called once");
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

            result.AssetTimeout(false);
            result.AssetOutput(
                "Connection opened",
                "Closed after 2000ms"
                );
            result.AssetErrors();

            // wait for message to be processed
            yield return new WaitForSeconds(0.2f);

            Assert.That(onConnect, Has.Count.EqualTo(1), "Connect should be called once");
            Assert.That(onDisconnect, Has.Count.EqualTo(1), "Disconnected should be called once");
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

            result.AssetTimeout(true);
            result.AssetOutput(
                "Connection opened"
                );
            result.AssetErrors();

            // wait for timeout
            yield return new WaitForSeconds(timeout / 1000);
            // give time to process message
            yield return new WaitForSeconds(1);

            Assert.That(onConnect, Has.Count.EqualTo(1), "Connect should be called once");
            Assert.That(onDisconnect, Has.Count.EqualTo(1), "Disconnected should be called once");
        }

        [UnityTest]
        public IEnumerator ShouldTimeoutClientAfterNoMessage()
        {
            // make sure doesn't timeout
            Task<RunNode.Result> task = RunNode.RunAsync("Connect.js", timeout * 2);

            // wait for timeout
            yield return new WaitForSeconds(timeout / 1000);
            // give time to process message
            yield return new WaitForSeconds(2);

            Assert.That(onConnect, Has.Count.EqualTo(1), "Connect should be called once");
            Assert.That(onDisconnect, Has.Count.EqualTo(1), "Disconnected should be called once");

            yield return new WaitForSeconds(0.2f);

            Assert.That(task.IsCompleted, Is.True, "Connect.js should have stopped after connection was closed by timeout");
            RunNode.Result result = task.Result;

            result.AssetTimeout(false);
            result.AssetOutput(
                "Connection opened",
                $"Connection closed"
                );
            result.AssetErrors();
        }
    }
}
