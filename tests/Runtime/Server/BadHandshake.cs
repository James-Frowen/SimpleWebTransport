using System;
using System.Collections;
using System.Net.Sockets;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Mirror.SimpleWeb.Tests.Server
{
    [Category("SimpleWebTransport")]
    public class BadHandshake : SimpleWebTestBase
    {
        TcpClient client;

        public override void TearDown()
        {
            base.TearDown();

            client?.Dispose();
        }

        [UnityTest]
        public IEnumerator ClosesConnectionIfBadData()
        {
            SimpleWebTransport transport = CreateRelayTransport();
            int onConnectedCalled = 0;
            int onDisconnectedCalled = 0;
            int onDataReceived = 0;
            transport.OnServerConnected.AddListener((_) => onConnectedCalled++);
            transport.OnServerDisconnected.AddListener((_) => onDisconnectedCalled++);
            transport.OnServerDataReceived.AddListener((_, __, ___) => onDataReceived++);
            transport.ServerStart();


            TcpClient client = new TcpClient();
            Task task = client.ConnectAsync("localhost", 7776);
            while (!task.IsCompleted)
            {
                yield return null;
            }

            Assert.That(onConnectedCalled, Is.EqualTo(0), "Connect should not be called");
            Assert.That(onDisconnectedCalled, Is.EqualTo(0), "Disconnect should not be called");
            Assert.That(onDataReceived, Is.EqualTo(0), "Data should not be called");
        }

        [UnityTest]
        public IEnumerator ClosesConnectionIfNoHandShakeInTimeout()
        {
            const int timeout = 4000;

            SimpleWebTransport transport = CreateRelayTransport();
            int onConnectedCalled = 0;
            int onDisconnectedCalled = 0;
            int onDataReceived = 0;
            transport.OnServerConnected.AddListener((_) => onConnectedCalled++);
            transport.OnServerDisconnected.AddListener((_) => onDisconnectedCalled++);
            transport.OnServerDataReceived.AddListener((_, __, ___) => onDataReceived++);
            transport.receiveTimeout = timeout;
            transport.ServerStart();

            TcpClient client = new TcpClient();

            Task task = Task.Run(() =>
            {
                try
                {
                    client.Connect("localhost", 7776);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            });
            while (!task.IsCompleted)
            {
                yield return null;
            }
            Assert.That(task.IsFaulted, Is.False, $"ConnectAsync should not have errors, instead had error:{task.Exception}");
            Assert.That(client.Connected, Is.True, "Client should have connected");

            yield return new WaitForSeconds(timeout / 1000);
            // wait for events
            yield return new WaitForSeconds(1);

            Assert.That(onConnectedCalled, Is.EqualTo(0), "Connect should not be called");
            Assert.That(onDisconnectedCalled, Is.EqualTo(0), "Disconnect should not be called");
            Assert.That(onDataReceived, Is.EqualTo(0), "Data should not be called");

            Assert.That(client.Connected, Is.False);

            // clean up
            client.Dispose();
        }
    }
}
