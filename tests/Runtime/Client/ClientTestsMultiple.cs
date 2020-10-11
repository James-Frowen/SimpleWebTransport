using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Mirror.SimpleWeb.Tests.Client
{
    [Category("SimpleWebTransport")]
    public class ClientTestsMultiple : SimpleWebTestBase
    {
        protected override bool StartServer => true;

        [UnityTest]
        public IEnumerator CanCreateMultipleClientInstances()
        {
            int count = 10;
            SimpleWebTransport[] transports = new SimpleWebTransport[count];

            for (int i = 0; i < count; i++)
            {
                transports[i] = CreateTransport<SimpleWebTransport>();

                transports[i].ClientConnect("localhost");
                yield return new WaitForSeconds(0.2f);
            }

            yield return new WaitForSeconds(0.5f);


            Assert.That(server.onConnect, Has.Count.EqualTo(count), $"Connect should have been calle {count} times");

            for (int i = 0; i < count; i++)
            {
                transports[i].ClientDisconnect();
                yield return new WaitForSeconds(0.2f);
            }

            // wait for disconnect
            yield return new WaitForSeconds(1);

            Assert.That(server.onDisconnect, Has.Count.EqualTo(count), $"disconnect should have been calle {count} times");
        }
    }
}
