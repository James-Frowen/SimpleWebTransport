const WebSocket = require("websocket").w3cwebsocket;

// Create webSocket connection.
const webSocket = new WebSocket("ws://localhost:7776/");
webSocket.binaryType = 'arraybuffer';

webSocket.addEventListener('error', function (event) {
    console.error('Socket Error', event);
});

const closeTimeout = 2000;
let closed = false;

// Connection opened
webSocket.addEventListener('open', function (event) {
    console.log('Connection opened');
    setTimeout(() => {
        if (!closed) {
            closed = true;
            console.log(`Closed after ${closeTimeout}ms`);
            webSocket.close(1000, `Closed after ${closeTimeout}ms`);
            setTimeout(() => {
                process.exit(0);
            }, 50);
        }
    }, closeTimeout);
});

webSocket.addEventListener('close', function (event) {
    // stop process in case close is called
    process.exit(0);
});