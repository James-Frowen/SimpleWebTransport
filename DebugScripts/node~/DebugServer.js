const WebSocketServer = require('websocket').server;
const https = require('https');
const fs = require('fs');

const options = {
    key: fs.readFileSync('./certs/MirrorLocal.key'),
    cert: fs.readFileSync('./certs/MirrorLocal.crt')
};

var server = https.createServer(options, function (request, response) {
    console.log((new Date()) + ' Received request for ' + request.url);
    response.writeHead(200);
    response.end("hello world\n");
});
server.listen(7776, function () {
    console.log('Server is listening on port 7776');
});

wsServer = new WebSocketServer({
    httpServer: server,

    // You should not use autoAcceptConnections for production
    // applications, as it defeats all standard cross-origin protection
    // facilities built into the protocol and the browser.  You should
    // *always* verify the connection's origin and decide whether or not
    // to accept it.
    autoAcceptConnections: false
});

wsServer.on('request', function (request) {
    console.log("request:", request);
    var connection = request.accept();
    console.log((new Date()) + ' Connection accepted.');

    connection.on('message', function (message) {
        console.log(`Received Message: ${message.type}`);
        if (message.type === 'utf8') {
            console.log('Received Message: ' + message.utf8Data);
            connection.sendUTF(message.utf8Data);
        }
        else if (message.type === 'binary') {
            console.log('Received Binary Message of ' + message.binaryData.length + ' bytes');
            connection.sendBytes(message.binaryData);
        }
    });
    connection.on('close', function (reasonCode, description) {
        console.log("close", reasonCode, description);
    });
    connection.on('error', function (err) {
        console.log("Error", err);
    });
});
