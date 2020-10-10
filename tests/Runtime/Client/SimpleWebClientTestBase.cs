using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Mirror.SimpleWeb.Tests.Client
{
    [Category("SimpleWebTransport")]
    public abstract class SimpleWebClientTestBase
    {
        protected const int timeout = 4000;
        const Log.Levels LogLevel = Log.Levels.info;

        protected abstract bool StartServer { get; }

        protected SimpleWebTransport server;
        protected List<int> server_onConnect = new List<int>();
        protected List<int> server_onDisconnect = new List<int>();
        protected List<(int connId, ArraySegment<byte> data)> server_onData = new List<(int connId, ArraySegment<byte> data)>();
        protected List<(int connId, Exception exception)> server_onError = new List<(int connId, Exception exception)>();

        protected SimpleWebTransport transport;
        // dont need list for connect/disconnect since there is no args for action
        protected int onConnect = 0;
        protected int onDisconnect = 0;
        protected List<ArraySegment<byte>> onData = new List<ArraySegment<byte>>();
        protected List<Exception> onError = new List<Exception>();

        protected WaitUntil WaitForConnect => new WaitUntil(() => onConnect >= 1);

        List<GameObject> toCleanup = new List<GameObject>();

        [SetUp]
        public virtual void SetUp()
        {
            Debug.Log($"SetUp {TestContext.CurrentContext.Test.Name}");

            transport = CreateTransport();

            onConnect = 0;
            onDisconnect = 0;
            onData.Clear();
            onError.Clear();

            transport.OnClientConnected.AddListener(() => onConnect++);
            transport.OnClientDisconnected.AddListener(() => onDisconnect++);
            transport.OnClientDataReceived.AddListener((data, _) => onData.Add(data));
            transport.OnClientError.AddListener((exception) => onError.Add(exception));


            server = CreateTransport();

            server_onConnect.Clear();
            server_onDisconnect.Clear();
            server_onData.Clear();
            server_onError.Clear();

            server.OnServerConnected.AddListener((connId) => server_onConnect.Add(connId));
            server.OnServerDisconnected.AddListener((connId) => server_onDisconnect.Add(connId));
            server.OnServerDataReceived.AddListener((connId, data, _) => server_onData.Add((connId, data)));
            server.OnServerError.AddListener((connId, exception) => server_onError.Add((connId, exception)));

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

        protected SimpleWebTransport CreateTransport()
        {
            GameObject go = new GameObject();
            toCleanup.Add(go);

            SimpleWebTransport transport = go.AddComponent<SimpleWebTransport>();
            transport.port = 7776;
            transport.logLevels = LogLevel;
            transport.receiveTimeout = timeout;
            transport.sendTimeout = timeout;

            Log.level = LogLevel;
            return transport;
        }
    }
}
