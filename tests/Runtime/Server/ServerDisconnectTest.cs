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
        protected override bool StartServer => true;

        [UnityTest]
        public IEnumerator CanKickConnection()
        {
            Task<RunNode.Result> task = RunNode.RunAsync("Disconnect.js");

            yield return server.WaitForConnection;

            server.ServerDisconnect(1);

            yield return new WaitUntil(() => task.IsCompleted);

            RunNode.Result result = task.Result;

            result.AssetTimeout(false);
            result.AssetOutput(
                "Connection closed"
                );
            result.AssetErrors();
        }
    }
}
