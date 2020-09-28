// copy paste this to console in chrome new tab
(() => {
    // set MirrorLocal in host file to point to ip of pc on local networks
    let webSocket = new WebSocket("ws://MirrorLocal:7776/");
    webSocket.binaryType = 'arraybuffer';

    const pingInterval = 1000;
    let intervalHandle;

    webSocket.addEventListener('error', function (event) {
        console.error('Socket Error', event);
    });

    webSocket.addEventListener('open', function (event) {
        console.log('Open event');
        intervalHandle = setInterval(() => {
            var buffer = new ArrayBuffer(4);
            var view = new Uint8Array(buffer);
            for (let i = 0; i < view.length; i++) {
                view[i] = i + 10;
            }
            console.log('Send Ping');
            webSocket.send(buffer);
        }, pingInterval);
    });

    webSocket.addEventListener('close', function (event) {
        console.log('Close event');
        clearInterval(intervalHandle);
    });

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
        return Array.prototype.map.call(new Uint8Array(buffer), x => ('00' + x.toString(16)).slice(-2)).join('-');
    }
})();
