const WebSocket = require("websocket").w3cwebsocket;
const RunMany = require("./RunMany").RunMany;

RunMany((onExit, log, error) => {
    // Create webSocket connection.
    const webSocket = new WebSocket("ws://localhost:7776/");
    webSocket.binaryType = 'arraybuffer';

    webSocket.addEventListener('error', function (event) {
        error(`Socket Error ${event}`);
    });

    const closeTimeout = 2000;
    let closed = false;

    // Connection opened
    webSocket.addEventListener('open', function (event) {
        log(`Connection opened`);
        setTimeout(() => {
            if (!closed) {
                closed = true;
                log(`Closed after ${closeTimeout}ms`);
                webSocket.close(1000, `Closed after ${closeTimeout}ms`);
                setTimeout(() => {
                    onExit();
                }, 50);
            }
        }, closeTimeout);
    });

    webSocket.addEventListener('close', function (event) {
        // stop process in case close is called
        onExit();
    });
});