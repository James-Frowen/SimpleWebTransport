const WebSocket = require("websocket").w3cwebsocket;

// Create webSocket connection.
const webSocket = new WebSocket("ws://localhost:7776/");
webSocket.binaryType = 'arraybuffer';

// Connection opened
webSocket.addEventListener('open', function (event) {
    console.log('Connection opened');
    setTimeout(() => {
        webSocket.close(1000, "end");
        process.exit(0);
    }, 1000);
});
