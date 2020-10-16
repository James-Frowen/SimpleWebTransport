// use this script to test if your cert is working

const https = require('https');
const fs = require('fs');
const port = 8000;

console.log("Listening on " + port);

const options = {
    pfx: fs.readFileSync('testCert.pfx')
};

https.createServer(options, (req, res) => {
    console.log("Request from " + (req.socket ? req.socket.remoteAddress : "NULL"));
    res.writeHead(200);
    res.end('hello world\n');
}).listen(port);
