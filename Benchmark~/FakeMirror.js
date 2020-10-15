const WebSocket = require("websocket").w3cwebsocket;

function ByteArray(count, hash1, hash2) {
    var buffer = new ArrayBuffer(count);
    var view = new Uint8Array(buffer);
    view[0] = hash1;
    view[1] = hash2;
    return buffer;
}
function NetworkPingMessage() {
    return ByteArray(10, 0x7F, 0x81);
}
function ReadyMessage() {
    return ByteArray(2, 0x3C, 0x9D)
}
function AddPlayerMessage() {
    return ByteArray(2, 0x1D, 0x33);
}
function Connect(address) {
    // set MirrorLocal in host file to point to ip of pc on local networks
    let webSocket = new WebSocket(address);
    webSocket.binaryType = 'arraybuffer';

    const pingInterval = 2000;
    let intervalHandle;

    webSocket.addEventListener('error', function (event) {
        console.error('Socket Error', event);
    });

    webSocket.addEventListener('open', function (event) {
        console.log('Connected to ' + address);
        var pingMsg = NetworkPingMessage();
        intervalHandle = setInterval(() => {
            webSocket.send(pingMsg);
        }, pingInterval);

        webSocket.send(ReadyMessage());
        webSocket.send(AddPlayerMessage());
    });

    webSocket.addEventListener('close', function (event) {
        console.log('Closed');
        clearInterval(intervalHandle);
    });

    webSocket.addEventListener('message', function (event) {
        var buffer = event.data;
        if (buffer instanceof ArrayBuffer) {
            // console.log(`length: ${buffer.byteLength} msg: ${buf2hex(buffer)}`);
        }
        else {
            console.error("Message not array buffer");
        }
    });

    function buf2hex(buffer) { // buffer is an ArrayBuffer
        return Array.prototype.map.call(new Uint8Array(buffer), x => ('00' + x.toString(16)).slice(-2)).join('-');
    }
}

function OpenMany(address, count) {
    for (let i = 0; i < count; i++) {
        setTimeout(() => {
            Connect(address);
        }, 10000 * i);
    }
}

const args = process.argv.slice(2);
const runCount = parseInt(args[0]);
OpenMany("ws://localhost:7776", runCount);
