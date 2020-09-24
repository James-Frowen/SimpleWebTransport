const WebSocket = require("websocket").w3cwebsocket;

// Create webSocket connection.
const webSocket = new WebSocket("ws://localhost:7776/");
webSocket.binaryType = 'arraybuffer';

webSocket.addEventListener('error', function (event) {
    console.error('Socket Error', event);
});

// Connection opened
webSocket.addEventListener('open', function (event) {
    var buffer = new ArrayBuffer(16385);
    var view = new Uint8Array(buffer);
    for (let i = 0; i < view.length; i++) {
        view[i] = i % 255;
    }

    webSocket.send(buffer);
});

webSocket.addEventListener('close', function (event) {
    process.exit(0);
});