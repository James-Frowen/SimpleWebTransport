using System;
using UnityEngine;

namespace JamesFrowen.SimpleWeb.Examples
{
    public class ExampleClient : MonoBehaviour
    {
        bool echo;
        private SimpleWebClient client;
        float keepAlive;

        private void Connect()
        {
            var tcpConfig = new TcpConfig(true, 5000, 5000);
            client = SimpleWebClient.Create(32000, 500, tcpConfig);

            client.onConnect += () => Debug.Log($"Connected to Server");
            client.onDisconnect += () => Debug.Log($"Disconnected from Server");
            client.onData += OnData;
            client.onError += (exception) => Debug.Log($"Error because of Server, Error:{exception}");

            client.Connect(new Uri("ws://localhost:7776"));
        }
        private void Update()
        {
            client?.ProcessMessageQueue();
            if (keepAlive < Time.time)
            {
                client?.Send(new ArraySegment<byte>(new byte[1] { 0 }));
                keepAlive = Time.time + 1;
            }
        }
        private void OnDestroy()
        {
            client?.Disconnect();
        }

        void OnData(ArraySegment<byte> data)
        {
            Debug.Log($"Data from Server, length:{data.Count}");
            if (echo)
            {
                if (client is WebSocketClientStandAlone standAlone)
                    standAlone.Send(data);
                else
                    client.Send(data);
            }
        }

        private void OnGUI()
        {
            if (GUILayout.Button("Connect"))
            {
                Connect();
            }

            echo = GUILayout.Toggle(echo, "Echo");
        }
    }
}
