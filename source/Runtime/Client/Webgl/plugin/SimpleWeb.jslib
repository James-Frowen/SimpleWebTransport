// this will create a global object
const SimpleWeb = {
    webSockets: new Map(),
    next: 1,
    GetWebSocket: function (index) {
        return SimpleWeb.webSockets.get(index);
    },
    AddNextSocket: function (webSocket) {
        var index = SimpleWeb.next;
        SimpleWeb.next++;
        SimpleWeb.webSockets.set(index, webSocket);
        return index;
    },
    RemoveSocket: function (index) {
        SimpleWeb.webSockets.delete(index);
    },
};

function SimpleWeb_IsConnected(index) {
    var webSocket = SimpleWeb.GetWebSocket(index);
    if (webSocket) {
        return webSocket.readyState === webSocket.OPEN;
    }
    else {
        return false;
    }
}

function SimpleWeb_Connect(addressPtr, openCallbackPtr, closeCallBackPtr, messageCallbackPtr, errorCallbackPtr, incomingDataBuffer, incomingDataBufferLength) {
    const address = UTF8ToString(addressPtr);
    console.log("Connecting to " + address);
    // Create webSocket connection.
    var webSocket = new WebSocket(address);
    webSocket.binaryType = 'arraybuffer';
    const index = SimpleWeb.AddNextSocket(webSocket);

    webSocket._incomingDataBufferAlive = true;

    // Connection opened
    webSocket.onopen = function (event) {
        console.log("Connected to " + address);
        {{{ makeDynCall('vi', 'openCallbackPtr') }}}(index);
    }
    webSocket.onclose = function (event) {
        console.log("Disconnected from " + address);
        {{{ makeDynCall('vi', 'closeCallBackPtr') }}}(index);
        webSocket._incomingDataBufferAlive = false;
    }

    // Listen for messages
    webSocket.onmessage = function (event) {
        if (event.data instanceof ArrayBuffer) {
            if (!webSocket._incomingDataBufferAlive) {
                console.error(`received message after disconnect`);
                return;
            }
            var array = new Uint8Array(event.data);
            var arrayLength = array.length;

            if (arrayLength > incomingDataBufferLength) {
                console.error(`Incoming message is too large: ${arrayLength} > ${incomingDataBufferLength}`);
                return;
            }

            var bufferPtr = incomingDataBuffer >>> 0; // Ensure unsigned 32-bit integer
            HEAPU8.set(array, bufferPtr);
            {{{ makeDynCall('viii', 'messageCallbackPtr') }}}(index, bufferPtr, arrayLength);
        }
        else {
            console.error("message type not supported")
        }
    }

    webSocket.onerror = function (event) {
        console.error('Socket Error', event);

        {{{ makeDynCall('vi', 'errorCallbackPtr') }}}(index);
    }

    return index;
}

function SimpleWeb_Disconnect(index) {
    var webSocket = SimpleWeb.GetWebSocket(index);
    if (webSocket) {
        webSocket._incomingDataBufferAlive = false;
        webSocket.close(1000, "Disconnect Called by SimpleWeb");
    }

    SimpleWeb.RemoveSocket(index);
}

function SimpleWeb_Send(index, arrayPtr, length) {
    var webSocket = SimpleWeb.GetWebSocket(index);
    if (webSocket) {
        const start = arrayPtr >>> 0; // Ensure unsigned 32-bit integer
        const end = start + length;
        const data = HEAPU8.slice(start, end);
        webSocket.send(data);
        return true;
    }
    return false;
}


const SimpleWebLib = {
    $SimpleWeb: SimpleWeb,
    SimpleWeb_IsConnected,
    SimpleWeb_Connect,
    SimpleWeb_Disconnect,
    SimpleWeb_Send
};
autoAddDeps(SimpleWebLib, '$SimpleWeb');
mergeInto(LibraryManager.library, SimpleWebLib);