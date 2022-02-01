using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace JamesFrowen.SimpleWeb.Examples
{
    public class ExampleServer : MonoBehaviour
    {
        private SimpleWebServer server;
        bool connection;
        Dictionary<int, byte[]> sent = new Dictionary<int, byte[]>();

        private IEnumerator Start()
        {
            var tcpConfig = new TcpConfig(true, 5000, 5000);

            server = new SimpleWebServer(5000, tcpConfig, 32000, 5000, default);
            server.Start(7776);

            server.onConnect += (id) => { connection = true; Debug.Log($"New Client connected, id:{id}"); };
            server.onDisconnect += (id) => Debug.Log($"Client disconnected, id:{id}");
            server.onData += OnData;
            server.onError += (id, exception) => Debug.Log($"Error because of Client, id:{id}, Error:{exception}");

            yield return new WaitUntil(() => connection);
            for (int i = 1; i < 200; i++)
            {
                yield return Send(i * 1000);
            }
        }
        private void Update()
        {
            server?.ProcessMessageQueue();
        }
        private void OnDestroy()
        {
            server?.Stop();
        }

        void OnData(int id, ArraySegment<byte> data)
        {
            Debug.Log($"Data from Client, id:{id}, length:{data.Count}");

            Assert.AreEqual(id, 1);
            byte[] received = data.Array;
            int length = data.Count;
            if (length == 1)
                return;
            byte[] bytes = sent[length];

            for (int i = 0; i < length; i++)
            {
                if (bytes[i] != received[i])
                    throw new Exception("Data not equal");
            }

            sent.Remove(length);
        }

        IEnumerator Send(int size)
        {
            byte[] bytes = new byte[size];
            var random = new System.Random();
            random.NextBytes(bytes);

            var segment = new ArraySegment<byte>(bytes);
            sent.Add(size, bytes);
            server.SendOne(1, segment);

            yield return new WaitForSeconds(0.5f);
        }
    }
}
