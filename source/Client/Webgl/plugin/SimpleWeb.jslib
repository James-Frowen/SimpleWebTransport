// this will create a global object
// IMPORTANT: functions must be declared in this global object, or exported at bottom of file
const SimpleWeb = {
    webSockets: [],
    next: 1,
    unityVersion: 0,
    GetWebSocket: function (index) {
        return SimpleWeb.webSockets[index]
    },
    AddNextSocket: function (webSocket) {
        var index = SimpleWeb.next;
        SimpleWeb.next++;
        SimpleWeb.webSockets[index] = webSocket;
        return index;
    },
    RemoveSocket: function (index) {
        SimpleWeb.webSockets[index] = undefined;
    },

    // calls to unity function that work with multiple version because unity likes to change them without documented it
    dynCall_vi: function (ptr, args) {
        if (typeof Runtime === "undefined") Runtime = { dynCall: dynCall }
        Runtime.dynCall('vi', ptr, args);
    },
    dynCall_viii: function (ptr, args) {
        if (typeof Runtime === "undefined") Runtime = { dynCall: dynCall }
        Runtime.dynCall('viii', ptr, args);
    },
    Init: function (unityVersion) {
        console.log("SimpleWeb Init with unityVersion:" + unityVersion);
        SimpleWeb.unityVersion = unityVersion;
    }
};

function IsConnected(index) {
    var webSocket = SimpleWeb.GetWebSocket(index);
    if (webSocket) {
        return webSocket.readyState === webSocket.OPEN;
    }
    else {
        return false;
    }
}

function Connect(addressPtr, openCallbackPtr, closeCallBackPtr, messageCallbackPtr, errorCallbackPtr) {
    const address = UTF8ToString(addressPtr);
    console.log("Connecting to " + address);
    // Create webSocket connection.
    webSocket = new WebSocket(address);
    webSocket.binaryType = 'arraybuffer';
    const index = SimpleWeb.AddNextSocket(webSocket);

    // Connection opened
    webSocket.addEventListener('open', function (event) {
        try {
            console.log("Connected to " + address);
            SimpleWeb.dynCall_vi(openCallbackPtr, [index]);
        } catch (e) { console.error(e); }
    });
    webSocket.addEventListener('close', function (event) {
        try {
            console.log("Disconnected from " + address);
            SimpleWeb.dynCall_vi(closeCallBackPtr, [index]);
        } catch (e) { console.error(e); }
    });

    // Listen for messages
    webSocket.addEventListener('message', function (event) {
        try {
            if (event.data instanceof ArrayBuffer) {
                // TODO dont alloc each time
                var array = new Uint8Array(event.data);
                var arrayLength = array.length;

                var bufferPtr = _malloc(arrayLength);
                var dataBuffer = new Uint8Array(HEAPU8.buffer, bufferPtr, arrayLength);
                dataBuffer.set(array);

                SimpleWeb.dynCall_viii(messageCallbackPtr, [index, bufferPtr, arrayLength]);
                _free(bufferPtr);
            }
            else {
                console.error("message type not supported");
            }
        } catch (e) {
            console.error(e);
        }
    });

    webSocket.addEventListener('error', function (event) {
        try {
            console.error('Socket Error', event);

            SimpleWeb.dynCall_vi(errorCallbackPtr, [index]);
        } catch (e) { console.error(e); }
    });

    return index;
}

function Disconnect(index) {
    var webSocket = SimpleWeb.GetWebSocket(index);
    if (webSocket) {
        webSocket.close(1000, "Disconnect Called by Mirror");
    }

    SimpleWeb.RemoveSocket(index);
}

function Send(index, arrayPtr, offset, length) {
    var webSocket = SimpleWeb.GetWebSocket(index);
    if (webSocket) {
        const start = arrayPtr + offset;
        const end = start + length;
        const data = HEAPU8.buffer.slice(start, end);
        webSocket.send(data);
        return true;
    }
    return false;
}

const SimpleWebLib = {
    $SimpleWeb: SimpleWeb,
    IsConnected,
    Connect,
    Disconnect,
    Send,
    Init: SimpleWeb.Init
};
autoAddDeps(SimpleWebLib, '$SimpleWeb');
mergeInto(LibraryManager.library, SimpleWebLib);