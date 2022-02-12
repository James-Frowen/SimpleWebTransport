using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Authentication;
using Mirror;
using UnityEngine;
using UnityEngine.Serialization;

namespace JamesFrowen.SimpleWeb.Tests
{
    public static class SimpleWebTransportExtension
    {
        /// <summary>
        /// Create copy of array, need to do this because buffers are re-used
        /// </summary>
        /// <param name="_"></param>
        /// <param name="segment"></param>
        /// <returns></returns>
        public static byte[] CreateCopy(this SimpleWebTransport _, ArraySegment<byte> segment)
        {
            byte[] copy = new byte[segment.Count];
            Array.Copy(segment.Array, segment.Offset, copy, 0, segment.Count);
            return copy;
        }
    }
    interface NeedInitTestInstance
    {
        void Init();
    }
    public class ServerTestInstance : SimpleWebTransport, NeedInitTestInstance
    {
        public readonly List<int> onConnect = new List<int>();
        public readonly List<int> onDisconnect = new List<int>();
        public readonly List<(int connId, byte[] data)> onData = new List<(int connId, byte[] data)>();
        public readonly List<(int connId, Exception exception)> onError = new List<(int connId, Exception exception)>();

        public void Init()
        {
            base.OnServerConnected = (connId) => onConnect.Add(connId);
            base.OnServerDisconnected = (connId) => onDisconnect.Add(connId);
            base.OnServerDataReceived = (connId, data, _) => onData.Add((connId, this.CreateCopy(data)));
            base.OnServerError = (connId, exception) => onError.Add((connId, exception));
        }

        public WaitUntil WaitForConnection => new WaitUntil(() => onConnect.Count >= 1);

        public void ServerSend(System.Collections.Generic.List<int> connectionIds, int channelId, ArraySegment<byte> segment)
        {
            foreach (int id in connectionIds)
            {
                ServerSend(id, channelId, segment);
            }
        }
    }
    public class ClientTestInstance : SimpleWebTransport, NeedInitTestInstance
    {
        public int onConnect = 0;
        public int onDisconnect = 0;
        public readonly List<byte[]> onData = new List<byte[]>();
        public readonly List<Exception> onError = new List<Exception>();

        public void Init()
        {
            base.OnClientConnected = () => onConnect++;
            base.OnClientDisconnected = () => onDisconnect++;
            base.OnClientDataReceived = (data, _) => onData.Add(this.CreateCopy(data));
            base.OnClientError = (exception) => onError.Add(exception);
        }

        public WaitUntil WaitForConnect => new WaitUntil(() => onConnect >= 1);
    }
}


namespace JamesFrowen.SimpleWeb
{
    public class SimpleWebTransport : Transport
    {
        public const string NormalScheme = "ws";
        public const string SecureScheme = "wss";

        [Tooltip("Port to use for server and client")]
        public ushort port = 7778;


        [Tooltip("Protect against allocation attacks by keeping the max message size small. Otherwise an attacker might send multiple fake packets with 2GB headers, causing the server to run out of memory after allocating multiple large packets.")]
        public int maxMessageSize = 16 * 1024;

        [Tooltip("Max size for http header send as handshake for websockets")]
        public int handshakeMaxSize = 3000;

        [Tooltip("disables nagle algorithm. lowers CPU% and latency but increases bandwidth")]
        public bool noDelay = true;

        [Tooltip("Send would stall forever if the network is cut off during a send, so we need a timeout (in milliseconds)")]
        public int sendTimeout = 5000;

        [Tooltip("How long without a message before disconnecting (in milliseconds)")]
        public int receiveTimeout = 20000;

        [Tooltip("Caps the number of messages the server will process per tick. Allows LateUpdate to finish to let the reset of unity contiue incase more messages arrive before they are processed")]
        public int serverMaxMessagesPerTick = 10000;

        [Tooltip("Caps the number of messages the client will process per tick. Allows LateUpdate to finish to let the reset of unity contiue incase more messages arrive before they are processed")]
        public int clientMaxMessagesPerTick = 1000;

        [Header("Server settings")]

        [Tooltip("Groups messages in queue before calling Stream.Send")]
        public bool batchSend = true;

        [Tooltip("Waits for 1ms before grouping and sending messages. " +
            "This gives time for mirror to finish adding message to queue so that less groups need to be made. " +
            "If WaitBeforeSend is true then BatchSend Will also be set to true")]
        public bool waitBeforeSend = false;


        [Header("Ssl Settings")]
        [Tooltip("Sets connect scheme to wss. Useful when client needs to connect using wss when TLS is outside of transport, NOTE: if sslEnabled is true clientUseWss is also true")]
        public bool clientUseWss;

        public bool sslEnabled;
        [Tooltip("Path to json file that contains path to cert and its password\n\nUse Json file so that cert password is not included in client builds\n\nSee cert.example.Json")]
        public string sslCertJson = "./cert.json";
        public SslProtocols sslProtocols = SslProtocols.Tls12;

        [Header("Debug")]
        [Tooltip("Log functions uses ConditionalAttribute which will effect which log methods are allowed. DEBUG allows warn/error, SIMPLEWEB_LOG_ENABLED allows all")]
        [FormerlySerializedAs("logLevels")]
        [SerializeField] Log.Levels _logLevels = Log.Levels.none;

        /// <summary>
        /// <para>Gets _logLevels field</para>
        /// <para>Sets _logLevels and Log.level fields</para>
        /// </summary>
        public Log.Levels LogLevels
        {
            get => _logLevels;
            set
            {
                _logLevels = value;
                Log.level = _logLevels;
            }
        }

        void OnValidate()
        {
            if (maxMessageSize > ushort.MaxValue)
            {
                Debug.LogWarning($"max supported value for maxMessageSize is {ushort.MaxValue}");
                maxMessageSize = ushort.MaxValue;
            }

            Log.level = _logLevels;
        }

        public SimpleWebClient client;
        public SimpleWebServer server;

        TcpConfig TcpConfig => new TcpConfig(noDelay, sendTimeout, receiveTimeout);

        public override bool Available()
        {
            return true;
        }
        public override int GetMaxPacketSize(int channelId = 0)
        {
            return maxMessageSize;
        }

        void Awake()
        {
            Log.level = _logLevels;
        }
        public override void Shutdown()
        {
            client?.Disconnect();
            client = null;
            server?.Stop();
            server = null;
        }

        void LateUpdate()
        {
            ProcessMessages();
        }

        /// <summary>
        /// Processes message in server and client queues
        /// <para>Invokes OnData events allowing mirror to handle messages (Cmd/SyncVar/etc)</para>
        /// <para>Called within LateUpdate, Can be called by user to process message before important logic</para>
        /// </summary>
        public void ProcessMessages()
        {
            server?.ProcessMessageQueue(this);
            client?.ProcessMessageQueue(this);
        }

        #region Client
        string GetClientScheme() => (sslEnabled || clientUseWss) ? SecureScheme : NormalScheme;
        string GetServerScheme() => sslEnabled ? SecureScheme : NormalScheme;
        public override bool ClientConnected()
        {
            // not null and not NotConnected (we want to return true if connecting or disconnecting)
            return client != null && client.ConnectionState != ClientState.NotConnected;
        }

        public override void ClientConnect(string hostname)
        {
            // connecting or connected
            if (ClientConnected())
            {
                Debug.LogError("Already Connected");
                return;
            }

            var builder = new UriBuilder
            {
                Scheme = GetClientScheme(),
                Host = hostname,
                Port = port
            };


            client = SimpleWebClient.Create(maxMessageSize, clientMaxMessagesPerTick, TcpConfig);
            if (client == null) { return; }

            client.onConnect += OnClientConnected.Invoke;
            client.onDisconnect += () =>
            {
                OnClientDisconnected.Invoke();
                // clear client here after disconnect event has been sent
                // there should be no more messages after disconnect
                client = null;
            };
            client.onData += (ArraySegment<byte> data) => OnClientDataReceived.Invoke(data, Channels.DefaultReliable);
            client.onError += (Exception e) =>
            {
                OnClientError.Invoke(e);
                ClientDisconnect();
            };

            client.Connect(builder.Uri);
        }

        public override void ClientDisconnect()
        {
            // dont set client null here of messages wont be processed
            client?.Disconnect();
        }

        public override void ClientSend(int channelId, ArraySegment<byte> segment)
        {
            if (!ClientConnected())
            {
                Debug.LogError("Not Connected");
                return;
            }

            if (segment.Count > maxMessageSize)
            {
                Log.Error("Message greater than max size");
                return;
            }

            if (segment.Count == 0)
            {
                Log.Error("Message count was zero");
                return;
            }

            client.Send(segment);
        }
        #endregion

        #region Server
        public override bool ServerActive()
        {
            return server != null && server.Active;
        }

        public override void ServerStart()
        {
            if (ServerActive())
            {
                Debug.LogError("SimpleWebServer Already Started");
            }

            SslConfig config = SslConfigLoader.Load(sslEnabled, sslCertJson, sslProtocols);
            server = new SimpleWebServer(serverMaxMessagesPerTick, TcpConfig, maxMessageSize, handshakeMaxSize, config);

            server.onConnect += OnServerConnected.Invoke;
            server.onDisconnect += OnServerDisconnected.Invoke;
            server.onData += (int connId, ArraySegment<byte> data) => OnServerDataReceived.Invoke(connId, data, Channels.DefaultReliable);
            server.onError += OnServerError.Invoke;

            SendLoopConfig.batchSend = batchSend || waitBeforeSend;
            SendLoopConfig.sleepBeforeSend = waitBeforeSend;

            server.Start(port);
        }

        public override void ServerStop()
        {
            if (!ServerActive())
            {
                Debug.LogError("SimpleWebServer Not Active");
            }

            server.Stop();
            server = null;
        }

        public override bool ServerDisconnect(int connectionId)
        {
            if (!ServerActive())
            {
                Debug.LogError("SimpleWebServer Not Active");
                return false;
            }

            return server.KickClient(connectionId);
        }

        public override void ServerSend(int connectionId, int channelId, ArraySegment<byte> segment)
        {
            if (!ServerActive())
            {
                Debug.LogError("SimpleWebServer Not Active");
                return;
            }

            if (segment.Count > maxMessageSize)
            {
                Log.Error("Message greater than max size");
                return;
            }

            if (segment.Count == 0)
            {
                Log.Error("Message count was zero");
                return;
            }

            server.SendOne(connectionId, segment);
            return;
        }

        public override string ServerGetClientAddress(int connectionId)
        {
            return server.GetClientAddress(connectionId);
        }

        public override Uri ServerUri()
        {
            var builder = new UriBuilder
            {
                Scheme = GetServerScheme(),
                Host = Dns.GetHostName(),
                Port = port
            };
            return builder.Uri;
        }
        #endregion
    }
}
