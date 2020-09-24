const WebSocket = require("websocket").w3cwebsocket;

// Create webSocket connection.
const webSocket = new WebSocket("ws://localhost:7776/");
webSocket.binaryType = 'arraybuffer';

webSocket.addEventListener('error', function (event) {
    console.error('Socket Error', event);
});

// Connection opened
webSocket.addEventListener('message', function (event) {
    var buffer = event.data;
    if (buffer instanceof ArrayBuffer) {
        console.log(`length: ${buffer.byteLength} msg: ${buf2hex(buffer)}`);
    }
    else {
        console.error("Message not array buffer");
    }
});

function buf2hex(buffer) { // buffer is an ArrayBuffer
    return Array.prototype.map.call(new Uint8Array(buffer), x => ('00' + x.toString(16)).slice(-2)).join(' ');
}

webSocket.addEventListener('close', function (event) {
    process.exit(0);
});