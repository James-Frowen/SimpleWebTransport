// use this script to test if your cert is working

const https = require('https');
const fs = require('fs');
const port = 8000;

const options = {
    pfx: fs.readFileSync('testCert.pfx')
};

https.createServer(options, (req, res) => {
    res.writeHead(200);
    res.end('hello world\n');
}).listen(port);
