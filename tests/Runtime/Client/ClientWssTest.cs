using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Mirror.SimpleWeb.Tests.Client
{
    [Ignore("Needs a CA key to work, see bottom of setup")]
    public class ClientWssTest : SimpleWebClientTestBase
    {
        protected override bool StartServer => false;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            server.sslEnabled = true;
            server.sslCertJson = "./Assets/SimpleWebTransport/source/.cert.example.Json";
            server.ServerStart();

            transport.sslEnabled = true;
        }

        [UnityTest]
        public IEnumerator Wss_CanConnectAndDisconnectFromServer()
        {
            transport.ClientConnect("localhost");

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

        [UnityTest]
        public IEnumerator Wss_CanPingAndStayConnectedForTime()
        {
            // server gets message and sends reply
            server.OnServerDataReceived.AddListener((i, data, __) =>
            {
                Assert.That(i, Is.EqualTo(1), "Conenction Id should be 1");

                byte[] expectedBytes = new byte[4] { 11, 12, 13, 14 };
                CollectionAssert.AreEqual(expectedBytes, data, "data should match");

                byte[] relyBytes = new byte[4] { 1, 2, 3, 4 };
                server.ServerSend(new List<int> { i }, 0, new ArraySegment<byte>(relyBytes));
            });
            transport.OnClientDataReceived.AddListener((data, __) =>
            {
                byte[] expectedBytes = new byte[4] { 1, 2, 3, 4 };

                CollectionAssert.AreEqual(expectedBytes, data, "data should match");
            });

            transport.ClientConnect("localhost");

            // wait for connect
            yield return new WaitForSeconds(1);

            for (int i = 0; i < 100; i++)
            {
                byte[] sendBytes = new byte[4] { 11, 12, 13, 14 };
                transport.ClientSend(0, new ArraySegment<byte>(sendBytes));
                yield return new WaitForSeconds(0.1f);
            }

            // wait for message
            yield return new WaitForSeconds(0.25f);


            Assert.That(onData, Has.Count.EqualTo(100), "client should have 100 messages");
            Assert.That(server_onData, Has.Count.EqualTo(100), "server should have 100 messages");
        }

    }
}
