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

namespace Mirror.SimpleWeb.Tests.Server
{
    [Category("SimpleWebTransport")]
    public abstract class SimpleWebServerTestBase
    {
        protected const int timeout = 4000;

        protected abstract bool StartServer { get; }

        protected SimpleWebTransport transport;
        protected List<int> onConnect = new List<int>();
        protected List<int> onDisconnect = new List<int>();
        protected List<(int connId, ArraySegment<byte> data)> onData = new List<(int connId, ArraySegment<byte> data)>();
        protected List<(int connId, Exception exception)> onError = new List<(int connId, Exception exception)>();

        protected WaitUntil WaitForConnect => new WaitUntil(() => onConnect.Count >= 1);

        List<GameObject> toCleanup = new List<GameObject>();

        [SetUp]
        public virtual void SetUp()
        {
            Debug.Log($"SetUp {TestContext.CurrentContext.Test.Name}");

            transport = CreateRelayTransport();

            onConnect.Clear();
            onDisconnect.Clear();
            onData.Clear();
            onError.Clear();

            transport.OnServerConnected.AddListener((connId) => onConnect.Add(connId));
            transport.OnServerDisconnected.AddListener((connId) => onDisconnect.Add(connId));
            transport.OnServerDataReceived.AddListener((connId, data, _) => onData.Add((connId, data)));
            transport.OnServerError.AddListener((connId, exception) => onError.Add((connId, exception)));

            if (StartServer)
            {
                transport.ServerStart();
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

        protected SimpleWebTransport CreateRelayTransport()
        {
            GameObject go = new GameObject();
            toCleanup.Add(go);

            SimpleWebTransport transport = go.AddComponent<SimpleWebTransport>();
            transport.port = 7776;
            transport.logLevels = Log.Levels.info;
            transport.receiveTimeout = timeout;
            transport.sendTimeout = timeout;

            Log.level = Log.Levels.info;
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
