let webSocket = undefined;

function IsConnected() {
    if (webSocket) {
        return webSocket.readyState === webSocket.OPEN;
    }
    else {
        return false;
    }
}


function Connect(addressPtr, openCallbackPtr, closeCallBackPtr, messageCallbackPtr, errorCallbackPtr) {
    const address = Pointer_stringify(addressPtr);
    console.log("Connecting to " + address);

    // Create webSocket connection.
    webSocket = new WebSocket(address);
    webSocket.binaryType = 'arraybuffer';

    // Connection opened
    webSocket.addEventListener('open', function (event) {
        console.log('Connection opened!');

        Runtime.dynCall('v', openCallbackPtr, 0);
        // webSocket.send('Hello Server!');
    });
    webSocket.addEventListener('close', function (event) {
        console.log('Socket Closed', event.data);

        Runtime.dynCall('v', closeCallBackPtr, 0);
    });

    // Listen for messages
    webSocket.addEventListener('message', function (event) {

        if (event.data instanceof ArrayBuffer) {
            // TODO dont alloc each time
            var array = new Uint8Array(event.data);
            var arrayLength = array.length;

            var bufferPtr = _malloc(arrayLength);
            var dataBuffer = new Uint8Array(HEAPU8.buffer, bufferPtr, arrayLength);
            dataBuffer.set(array);

            console.log("Message length " + arrayLength.toString());
            Runtime.dynCall('vii', messageCallbackPtr, [bufferPtr, arrayLength]);
            _free(bufferPtr);
        }
        else {
            console.error("message type not supported")
        }
    });

    webSocket.addEventListener('error', function (event) {
        console.error('Socket Error', event.data);

        Runtime.dynCall('v', errorCallbackPtr, 0);
    });
}

function Disconnect() {
    console.log("Disconnect");

    if (webSocket) {
        webSocket.close(1000, "Disconnect Called by Mirror");
    }

    webSocket = undefined;
}

function Send(arrayPtr, offset, length) {
    console.log("Send Array, offset:" + offset + " length:" + length);
    for (i = 0; i < length; i++) {
        console.log("    " + HEAPU8[arrayPtr + offset + i]);
    }

    if (webSocket) {
        const start = arrayPtr + offset;
        const end = start + length;
        const data = HEAPU8.buffer.slice(start, end);
        webSocket.send(data);
    }
}
function InvokeCallback(callbackPtr) {
    Runtime.dynCall('v', callbackPtr, 0);
}

function Debug(str) {
    window.alert(Pointer_stringify(str));
}


mergeInto(LibraryManager.library, {
    DebugNone: Debug,
    DebugAnsi: Debug,
    DebugUnicode: Debug,
    DebugAuto: Debug,
    IsConnected,
    Connect,
    Disconnect,
    Send,
    InvokeCallback
});