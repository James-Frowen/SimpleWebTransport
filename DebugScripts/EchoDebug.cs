using System;
using System.Threading.Tasks;
using JamesFrowen.SimpleWeb;
using UnityEngine;

public class EchoDebug : MonoBehaviour
{
    class Token
    {
        public bool stop;
    }

    Token token;
    int counter;

    private void OnGUI()
    {
        if (token == null)
        {
            if (GUILayout.Button("Start"))
            {
                StartWebSocket();
            }
        }
        else
        {
            if (GUILayout.Button("Stop"))
            {
                token.stop = true;
            }
        }
    }

    void StartWebSocket()
    {
        token = new Token();
#if UNITY_EDITOR
        _ = ServerServer();
#else
        _ = StartClient();
#endif
    }

    private async Task ServerServer()
    {
        try
        {

            // create server instance
            var tcpConfig = new TcpConfig(noDelay: false, sendTimeout: 5000, receiveTimeout: 20000);
            var server = new SimpleWebServer(5000, tcpConfig, ushort.MaxValue, 5000, new SslConfig());

            // listen for events
            server.onConnect += (id) => Debug.Log($"New Client connected, id:{id}");
            server.onDisconnect += (id) => Debug.Log($"Client disconnected, id:{id}");
            server.onData += (id, data) =>
            {
                Debug.Log($"Data from Client, id:{id}, {BitConverter.ToString(data.Array, data.Offset, data.Count)}");
                // pong
                server.SendOne(id, data);
            };
            server.onError += (id, exception) => Debug.Log($"Error because of Client, id:{id}, Error:{exception}");

            // start server listening on port 7777
            server.Start(7777);

            while (true)
            {
                server.ProcessMessageQueue();
                await Task.Yield();

                if (token.stop) break;
            }

            token = null;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private async Task StartClient()
    {
        try
        {
            var client = SimpleWebClient.Create(32000, 1000, default);
            client.Connect(new UriBuilder()
            {
                Host = "localhost",
                Port = 7777,
                Scheme = "ws"
            }.Uri);

            client.onConnect += () => Debug.Log($"Connected to Server");
            client.onDisconnect += () => Debug.Log($"Disconnected from Server");
            client.onData += (data) => Debug.Log($"Data from Server, {BitConverter.ToString(data.Array, data.Offset, data.Count)}");
            client.onError += (exception) => Debug.Log($"Error because of Server, Error:{exception}");

            while (true)
            {
                client.ProcessMessageQueue();
                // ping
                client.Send(new ArraySegment<byte>(new byte[1] { (byte)(counter++) }));
                await Task.Yield();

                if (token.stop) break;
            }

            token = null;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }
}
