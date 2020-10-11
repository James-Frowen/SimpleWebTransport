using System;
using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Mirror.SimpleWeb.Tests.Server
{
    [Category("SimpleWebTransport")]
    [Ignore("Needs a CA key to work, see bottom of setup")]
    public class SslServerTest : SimpleWebTestBase
    {
        protected override bool StartServer => false;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            server.sslEnabled = true;
            server.sslCertJson = "./Assets/SimpleWebTransport/source/.cert.example.Json";
            server.ServerStart();

            // to use these test you need to create a CA cert and use it to sign MirrorLocal
            // then add the cert to node so that it will accept it
            Environment.SetEnvironmentVariable("NODE_EXTRA_CA_CERTS", "path/to/CACert.pem");
        }

        [UnityTest]
        public IEnumerator ClientCanConnectOverWss()
        {
            Task<RunNode.Result> task = RunNode.RunAsync("WssConnectAndClose.js");
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

            Assert.That(server.onConnect, Has.Count.EqualTo(1), "Connect should be called once");
        }
    }
}


