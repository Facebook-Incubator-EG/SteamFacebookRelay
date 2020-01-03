const login = require("facebook-chat-api");
const fs = require('fs');
const net = require('net');

const config = JSON.parse(fs.readFileSync("config.json").toString());

if(config.use_facebook) {

    // Create simple echo bot
    login({email: "battlechickenchatbot@gmail.com", password: config.password}, (err, api) => {
        if(err) return console.error(err);

        api.listen((err, message) => {
            console.log(message.body);
            api.sendMessage(message.body, message.threadID);
        });
    });

}





const pipeBaseName = "\\\\.\\pipe\\";
const outboundPipeName = pipeBaseName + 'fb_steam_pipe'
const inboundPipeName = pipeBaseName + 'steam_fb_pipe'

var L = console.log;



var server = net.createServer(function(stream) {
    L('Server: on connection')

    stream.on('data', function(c) {
        L('Server: on data:', c.toString());
        stream.write("Koszike");
    });

    stream.on('end', function() {
        L('Server: on end')
        server.close();
    });

    stream.write('Take it easy!');
});

server.on('close',function(){
    L('Server: on close');
})

server.on('error',function(error){
    L(`Server error: ${error.toString()} `);
})

server.listen(inboundPipeName,function(){
    L('Server: on listening');
})

// // == Client part == //
// var client = net.createConnection(inboundPipeName, function() {
//     L('Client: on connection');
// })


// client.on('data', function(data) {
//     L('Client: on data:', data.toString());
//     client.end('Thanks!');
// });

// client.on('end', function() {
//     L('Client: on end');
// })

// client.on('error', function(error) {
//     L('Error: ' + error.toString());
// })
// client.connect();



