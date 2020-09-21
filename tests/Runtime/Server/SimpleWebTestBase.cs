using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Mirror.SimpleWeb.Tests.Server
{
    public class SimpleWebTestBase
    {
        List<GameObject> toCleanup = new List<GameObject>();

        [TearDown]
        public virtual void TearDown()
        {
            foreach (GameObject obj in toCleanup)
            {
                if (obj != null)
                {
                    Transport transport;
                    if ((transport = obj.GetComponent<Transport>()) != null)
                    {
                        transport.Shutdown();
                    }
                    GameObject.DestroyImmediate(obj);
                }
            }
        }

        protected SimpleWebTransport CreateRelayTransport()
        {
            GameObject go = new GameObject();
            toCleanup.Add(go);

            SimpleWebTransport transport = go.AddComponent<SimpleWebTransport>();
            transport.port = 7776;
            return transport;
        }
    }
}
