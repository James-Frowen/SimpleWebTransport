const WebSocket = require("websocket").w3cwebsocket;

// Create webSocket connection.
const webSocket = new WebSocket("ws://localhost:7776/");
webSocket.binaryType = 'arraybuffer';

// Connection opened
webSocket.addEventListener('close', function (event) {
    console.log('Connection closed');
    process.exit(0);
});