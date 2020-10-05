const WebSocket = require("websocket").w3cwebsocket;

// Create webSocket connection.
const webSocket = new WebSocket("ws://localhost:7776/");
webSocket.binaryType = 'arraybuffer';

webSocket.addEventListener('error', function (event) {
    console.error('Socket Error', event);
});

// Connection opened
webSocket.addEventListener('open', function (event) {
    var buffer = new ArrayBuffer(16384);
    var view = new Uint8Array(buffer);
    for (let i = 0; i < view.length; i++) {
        view[i] = i % 255;
    }
    // send 100 message 20ms apart
    for (let i = 0; i < 100; i++) {

        setTimeout(() => {
            view[0] = i;
            webSocket.send(buffer);
        }, i * 20);
    }
    setTimeout(() => {
        webSocket.close
    }, (100 * 20) + 1000);
});

webSocket.addEventListener('close', function (event) {
    process.exit(0);
});