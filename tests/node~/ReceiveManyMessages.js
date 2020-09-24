const WebSocket = require("websocket").w3cwebsocket;

// Create webSocket connection.
const webSocket = new WebSocket("ws://localhost:7776/");
webSocket.binaryType = 'arraybuffer';

webSocket.addEventListener('error', function (event) {
    console.error('Socket Error', event);
});

// use ping to keep connection alive
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

// let msgCount = 0;
// Connection opened
webSocket.addEventListener('message', function (event) {
    var buffer = event.data;
    if (buffer instanceof ArrayBuffer) {
        // msgCount++;
        // console.log(msgCount);
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