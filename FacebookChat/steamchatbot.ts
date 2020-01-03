//declare function require(name:string);

import login = require("facebook-chat-api");
import fs = require('fs');
import net = require('net');

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





const pipeName = '\\\\.\\pipe\\steam_fb_pipe';

fs.open(pipeName, fs.constants.O_RDWR | fs.constants.O_NONBLOCK, (err, fd) => {
    // Handle err
    const pipe = new net.Socket({ fd });
    // Now `pipe` is a stream that can be used for reading from the FIFO.
    pipe.on('data', (data) => {
        // process data ...
    });
});


