using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Mirror.SimpleWeb.Tests.Client
{
    [Category("SimpleWebTransport")]
    public class ClientConnectAndCloseTest : SimpleWebClientTestBase
    {
        protected override bool StartServer => true;

        [UnityTest]
        public IEnumerator CanConnectToServer()
        {
            transport.ClientConnect("ws://localhost:7776/");

            // wait for connect
            yield return new WaitForSeconds(1);

            Assert.That(onConnect, Is.EqualTo(1), "Connect should be called once");
            Assert.That(server_onConnect, Has.Count.EqualTo(1), "server Connect should be called once");

            transport.ClientDisconnect();

            // wait for disconnect
            yield return new WaitForSeconds(1);

            Assert.That(onDisconnect, Is.EqualTo(1), "Disconnect should be called once");
            Assert.That(server_onDisconnect, Has.Count.EqualTo(1), "server Disconnect should be called once");
        }
    }
}
