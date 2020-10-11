using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Mirror.SimpleWeb.Tests
{
    [Category("SimpleWebTransport")]
    public abstract class SimpleWebTestBase
    {
        protected const int timeout = 4000;
        const Log.Levels LogLevel = Log.Levels.info;

        protected abstract bool StartServer { get; }

        protected ServerTestInstance server;
        protected ClientTestInstance client;

        List<GameObject> toCleanup = new List<GameObject>();

        [SetUp]
        public virtual void SetUp()
        {
            Debug.Log($"SetUp {TestContext.CurrentContext.Test.Name}");

            server = CreateTransport<ServerTestInstance>();
            client = CreateTransport<ClientTestInstance>();

            if (StartServer)
            {
                server.ServerStart();
            }
        }


        [TearDown]
        public virtual void TearDown()
        {
            Debug.Log($"TearDown {TestContext.CurrentContext.Test.Name}");

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

        protected T CreateTransport<T>() where T : SimpleWebTransport
        {
            GameObject go = new GameObject();
            toCleanup.Add(go);

            T transport = go.AddComponent<T>();
            transport.port = 7776;
            transport.logLevels = LogLevel;
            transport.receiveTimeout = timeout;
            transport.sendTimeout = timeout;

            Log.level = LogLevel;

            if (transport is NeedInitTestInstance needInit)
            {
                needInit.Init();
            }
            return transport;
        }

        protected static Task<TcpClient> CreateBadClient()
        {
            return Task.Run<TcpClient>(() =>
            {
                try
                {
                    TcpClient client = new TcpClient
                    {
                        SendTimeout = 1000,
                        ReceiveTimeout = 1000
                    };

                    client.Connect("localhost", 7776);

                    return client;
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                return null;
            });
        }

        protected static bool HasDisconnected(TcpClient client)
        {
            bool resetOrHasData = client.Client.Poll(-1, SelectMode.SelectRead);
            bool noData = client.Available == 0;
            bool reset = resetOrHasData && noData;

            return reset;
        }

        protected static void WriteBadData(TcpClient client)
        {
            byte[] buffer = Enumerable.Range(1, 10).Select(x => (byte)x).ToArray();
            try
            {
                client.GetStream().Write(buffer, 0, 10);
            }
            catch (IOException) { }
        }

        protected void ExpectHandshakeFailedError()
        {
            LogAssert.Expect(LogType.Error, new Regex("ERROR: <color=red>Handshake Failed.*"));
        }

        protected void ExpectInvalidDataError()
        {
            LogAssert.Expect(LogType.Error, new Regex(@"ERROR: <color=red>Invalid data from \[Conn:1"));
        }
    }
}
