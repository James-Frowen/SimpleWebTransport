const WebSocket = require("websocket").w3cwebsocket;

// Create webSocket connection.
const webSocket = new WebSocket("ws://localhost:7776/");
webSocket.binaryType = 'arraybuffer';

webSocket.addEventListener('error', function (event) {
    console.error('Socket Error', event);
});

const pingInterval = 1000;
// Connection opened
webSocket.addEventListener('open', function (event) {
    setInterval(() => {
        var buffer = new ArrayBuffer(4);
        var view = new Uint8Array(buffer);
        for (let i = 0; i < view.length; i++) {
            view[i] = i + 10;
        }
        webSocket.send(buffer);
    }, pingInterval);
});

webSocket.addEventListener('close', function (event) {
    process.exit(0);
});