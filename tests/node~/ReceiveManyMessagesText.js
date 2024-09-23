const WebSocket = require("websocket").w3cwebsocket;

// Create WebSocket connection.
const webSocket = new WebSocket("ws://localhost:7776/");

webSocket.addEventListener('error', function (event) {
    console.error('Socket Error', event);
});

// Use ping to keep connection alive
const pingInterval = 1000;
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

webSocket.addEventListener('message', function (event) {
    var message = event.data;
    if (typeof message === 'string') {
        console.log(`Received text message: ${message}`);
    } else {
        console.error("Message not a string");
    }
});

webSocket.addEventListener('close', function (event) {
    process.exit(0);
});
