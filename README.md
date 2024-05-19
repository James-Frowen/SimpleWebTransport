<!-- [![Coverage](https://sonarcloud.io/api/project_badges/measure?project=MirrorNetworking_SimpleWebTransport&metric=coverage)](https://sonarcloud.io/dashboard?id=MirrorNetworking_SimpleWebTransport) 
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=MirrorNetworking_SimpleWebTransport&metric=alert_status)](https://sonarcloud.io/dashboard?id=MirrorNetworking_SimpleWebTransport) -->
[![Discord](https://img.shields.io/discord/809535064551456888.svg)](https://discordapp.com/invite/yp6W73Xs68)
[![release](https://img.shields.io/github/release/James-Frowen/SimpleWebTransport.svg)](https://github.com/James-Frowen/SimpleWebTransport/releases/latest)
[![openupm](https://img.shields.io/npm/v/com.james-frowen.simplewebtransport?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.james-frowen.simplewebtransport/)

# Simple Web Transport

Low level Websocket Transport use by [Mirror](https://github.com/vis2k/Mirror) and [Mirage](https://github.com/MirageNet/Mirage)

This Transport uses the websocket protocol. This allows this transport to be used in WebGL builds of unity.

Includes a Websocket server, standalone client, and a WebGL client so that both your Server and Client can be build with Unity.

## Download

#### UnityPackage 

1) Download the code from the source folder or package on [Release](https://github.com/James-Frowen/SimpleWebTransport/releases) page.
2) Put the code somewhere in your Assets folder

#### openUPM

1) Add openupm registry.  Click on the menu Edit -> Project settings...,  and add a scoped registry like so: <br/>
    Name: `OpenUPM` <br/>
    Url: `https://package.openupm.com` <br/>
    Scopes:
    - `com.james-frowen`
2) Close the project settings
3) Open the package manager.  
4) Click on menu Window -> Package Manager and select "Packages: My Registries", 
5) select the latest version of `SimpleWebTransport` and click install, like so:
6) You may come back to the package manager to unistall `SimpleWebTransport` or upgrade it.

Or add to `Packages/manifest.json`

```json
{
    "dependencies": {
        "com.james-frowen.simplewebtransport": "2.2.0"
    },
    "scopedRegistries": [
        {
            "name": "package.openupm.com",
            "url": "https://package.openupm.com",
            "scopes": [
                "com.james-frowen.simplewebtransport"
            ]
        }
    ]
}
```

## Usage

Below are some examples of how to set up a server and client so that they will connect and low any message sent between them

#### Server

```cs
// create server instance
var tcpConfig = new TcpConfig(noDelay: false, sendTimeout: 5000, receiveTimeout: 20000);
var server = new SimpleWebServer(5000, tcpConfig, ushort.MaxValue, 5000, new SslConfig());

// listen for events
server.onConnect += (id) => Debug.Log($"New Client connected, id:{id}");
server.onDisconnect += (id) => Debug.Log($"Client disconnected, id:{id}");
server.onData += (id, data) => Debug.Log($"Data from Client, id:{id}, {BitConverter.ToString(data.Array, data.Offset, data.Count)})");
server.onError += (id, exception) => Debug.Log($"Error because of Client, id:{id}, Error:{exception}");

// start server listening on port 7777
server.Start(7777);

// call Process to cause events to be invoked
// Call this from inside Unity Update method so that it will process message each frame
server.ProcessMessageQueue();
```

#### Client
```cs
// create client instance
// call static SimpleWebClient.Create method so that the correct client for WebGL or standalone is created
var tcpConfig = new TcpConfig(noDelay: false, sendTimeout: 5000, receiveTimeout: 20000);
var client = SimpleWebClient.Create(ushort.MaxValue, 5000, tcpConfig);

// listen for events
client.onConnect += () => Debug.Log($"Connected to Server");
client.onDisconnect += () => Debug.Log($"Disconnected from Server");
client.onData += (data) => Debug.Log($"Data from Server, {BitConverter.ToString(data.Array, data.Offset, data.Count)})");
client.onError += (exception) => Debug.Log($"Error because of Server, Error:{exception}");

// create url and connect to server
var builder = new UriBuilder
{
    Scheme = "ws",
    Host = "www.example.com",
    Port = 7777
};

client.Connect(builder.Uri);

// call Process to cause events to be invoked
// Call this from inside Unity Update method so that it will process message each frame
client.ProcessMessageQueue();
```

#### Send message to Server

Once the client is connected (after the onConnect event or check `ConnectionState`.) message can be sent
```cs
byte[] message = Encoding.ASCII.GetBytes("Hello Server");
client.Send(new ArraySegment<byte>(message));
```

Most of the time you will want to create a message Id so that the server can know what the message should be.
But for this example we just send a string, if using the server example above it will log the bytes from the message when it is received


## Bugs?

Please report any bugs or issues [Here](https://github.com/James-Frowen/SimpleWebTransport/issues)


# Websocket Secure

This transport supports the wss protocol which is required for https pages.

## Reverse proxy

Using a reverse proxy for SSL is generally more efficient than implementing SSL directly within Unity or other game servers, as the proxy can specialize in handling encryption, freeing up the game server to focus on delivering smooth gameplay.

To use reverse proxy with Mirror see this page [Mirror/README.md](https://github.com/James-Frowen/SimpleWebTransport/blob/master/Mirror/README.md)

#### Nginx

Nginx is a popular reverse proxy thaat is easy to set up, and also has the ability to host WebGL files making it easy to set up development server. For a short guide on Nginx see this page: [NginxConfig](https://github.com/James-Frowen/SimpleWebTransport/tree/master/NginxConfig)

#### Other reverse proxies

The [Mirror docs](https://mirror-networking.gitbook.io/docs/manual/transports/websockets-transport/reverse-proxy) explains how to set up some othher revierse proxies, 


## (not recommended) How to create and setup an SSL Cert

If you host your webgl build on a https domain you will need to use wss which will require a ssl cert.

[See this page](./HowToCreateSSLCert.md)


# Logging and Debugging

Logging is disabled by default to increase performance.

Log levels can be set by using `Log.level = Log.Levels.warn`. 

### Log methods

Log methods in this transport use the [ConditionalAttribute](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.conditionalattribute?view=netstandard-2.0) so they are removed depending on the preprocessor defines.

These preprocessor defines effect the logging
- `DEBUG` allows warn/error logs 
- `SIMPLEWEB_LOG_ENABLED` allows all logs

Without `SIMPLEWEB_LOG_ENABLED` info or verbose logging will never happen even if log level allows it.

See the [Unity docs](https://docs.unity3d.com/Manual/PlatformDependentCompilation.html) on how set custom defines.
