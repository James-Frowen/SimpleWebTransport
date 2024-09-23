const WebSocket = require("websocket").w3cwebsocket;

// Create WebSocket connection.
const webSocket = new WebSocket("ws://localhost:7776/");

webSocket.addEventListener('error', function (event) {
    console.error('Socket Error', event);
});

// Connection opened
webSocket.addEventListener('open', function (event) {
    // send 100 text messages as fast as possible
    for (let i = 0; i < 100; i++) {
        webSocket.send(`Message ${i}`);
    }
});

webSocket.addEventListener('close', function (event) {
    process.exit(0);
});
