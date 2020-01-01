const login = require("facebook-chat-api");
const fs = require('fs');
const net = require('net');

const password = fs.readFileSync("password.txt").toString().trim();

// Create simple echo bot
login({email: "battlechickenchatbot@gmail.com", password: password}, (err, api) => {
    if(err) return console.error(err);

    api.listen((err, message) => {
        console.log(message.body);
        api.sendMessage(message.body, message.threadID);
    });
});




const pipeName = '\\\\.\\pipe\\';

fs.open(pipeName, fs.constants.O_RDONLY | fs.constants.O_NONBLOCK, (err, fd) => {
    // Handle err
    const pipe = new net.Socket({ fd });
    // Now `pipe` is a stream that can be used for reading from the FIFO.
    pipe.on('data', (data) => {
        // process data ...
    });
});


