let websocket;

function IsConnected() {
    return websocket?.readyState === WebSocket.OPEN;
}

function Connect(addressPtr, openCallbackPtr, closeCallBackPtr, messageCallbackPtr, errorCallbackPtr) {
    const address = Pointer_stringify(addressPtr);
    console.log("Connecting to " + address);

    // Create WebSocket connection.
    const websocket = new WebSocket(address);

    // Connection opened
    websocket.addEventListener('open', function (event) {
        console.log('Connection opened!');

        Runtime.dynCall('v', openCallbackPtr, 0);
        // websocket.send('Hello Server!');
    });
    websocket.addEventListener('close', function (event) {
        console.log('Socket Closed', event.data);

        Runtime.dynCall('v', closeCallBackPtr, 0);
    });
    // Listen for messages
    websocket.addEventListener('message', function (event) {
        console.log('Message from server ', event.data);

        if (e.data instanceof ArrayBuffer) {
            var array = new Uint8Array(e.data);
            var ptr = _malloc(array.length);
            var dataHeap = new Uint8Array(HEAPU8.buffer, ptr, array.length);
            dataHeap.set(array);
            Runtime.dynCall('vii', messageCallbackPtr, [ptr, array.length]);
            _free(ptr);
        }
        else {
            console.error("message type not supported")
        }
    });
    websocket.addEventListener('error', function (event) {
        console.error('Socket Error', event.data);

        Runtime.dynCall('v', errorCallbackPtr, 0);
    });
}

function Disconnect() {
    console.log("Disconnect");

    if (websocket)
        websocket.close();

    websocket = undefined;
}

function Send(arrayPtr, offset, length) {
    console.log("Send Array, offset:" + offset + " length:" + length);
    for (i = 0; i < length; i++) {
        console.log("    " + HEAPU8[arrayPtr + offset + i]);
    }

    if (websocket) {
        const start = arrayPtr + offset;
        const end = start + length;
        const data = HEAPU8.buffer.slice(start, end);
        websocket.send(data);
    }
}
function InvokeCallback(callbackPtr) {
    Runtime.dynCall('v', callbackPtr, 0);
}


mergeInto(LibraryManager.library, {
    IsConnected,
    Connect,
    Disconnect,
    Send,
    InvokeCallback
});