using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Mirror.SimpleWeb.Tests.Server
{
    [Category("SimpleWebTransport")]
    public class StartAndStopTest : SimpleWebTestBase
    {
        protected override bool StartServer => false;

        [UnityTest]
        public IEnumerator ServerCanStartAndStopWithoutErrors()
        {
            SimpleWebTransport transport = CreateTransport<SimpleWebTransport>();

            transport.ServerStart();
            Assert.That(transport.ServerActive(), Is.True);
            yield return new WaitForSeconds(0.2f);
            Assert.That(transport.ServerActive(), Is.True);

            transport.ServerStop();
            Assert.That(transport.ServerActive(), Is.False);
            yield return new WaitForSeconds(0.2f);
            Assert.That(transport.ServerActive(), Is.False);
        }


        [UnityTest]
        public IEnumerator CanStart2ndServerAfterFirstSTops()
        {
            // use {} block for local variable scope
            {
                SimpleWebTransport transport = CreateTransport<SimpleWebTransport>();

                transport.ServerStart();
                Assert.That(transport.ServerActive(), Is.True);
                yield return new WaitForSeconds(0.2f);
                Assert.That(transport.ServerActive(), Is.True);

                transport.ServerStop();
                Assert.That(transport.ServerActive(), Is.False);
            }

            {
                SimpleWebTransport transport = CreateTransport<SimpleWebTransport>();

                transport.ServerStart();
                Assert.That(transport.ServerActive(), Is.True);
                yield return new WaitForSeconds(0.2f);
                Assert.That(transport.ServerActive(), Is.True);

                transport.ServerStop();
                Assert.That(transport.ServerActive(), Is.False);
            }
        }
    }
}
