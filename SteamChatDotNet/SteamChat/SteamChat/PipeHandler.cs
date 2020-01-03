using System;
using System.Collections.Generic;
using System.Text;
using SteamKit2.Unified.Internal;

using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Text;
using System.Threading;

namespace SteamChat {

    

    class PipeHandler {

        static String outboundPipeName = "steam_fb_pipe";
        static String inboundPipeName = "fb_steam_pipe";

        public delegate void OnFacebookMessageReceived(string message);

        NamedPipeClientStream PipeStream;
        

        public PipeHandler() {
            new Thread(HandleCommunication).Start();

        }

        void Connect() {

            PipeStream = new NamedPipeClientStream(".", outboundPipeName,
                           PipeDirection.InOut, PipeOptions.None,
                           TokenImpersonationLevel.Impersonation);

            Console.WriteLine("Connecting to pipe...\n");
            PipeStream.Connect();
            Console.WriteLine("Successfully connected to pipe!\n");
        }

        void HandleCommunication() {

            while(true) {
                Connect();

                while(PipeStream.IsConnected) {
                    byte[] buffer = new byte[1024 * 10];
                    int bytesRead = PipeStream.Read(buffer);

                    string message = encoding.GetString(buffer, 0, bytesRead);

                    Console.WriteLine($">{message}");
                }

            }
            
            


            //var ss = new StreamString(pipeClient);
            //// Validate the server's signature string.
            //if (ss.ReadString() == "I am the one true server!") {
            //    // The client security token is sent with the first write.
            //    // Send the name of the file whose contents are returned
            //    // by the server.
            //    ss.WriteString("c:\\textfile.txt");

            //    // Print the file to the screen.
            //    Console.Write(ss.ReadString());
            //} else {
            //    Console.WriteLine("Server could not be verified.");
            //}

            //pipeClient.Close();
            // Give the client process some time to display results before exiting.
            //Thread.Sleep(4000);
        }

        
        private Encoding encoding = new UTF8Encoding();

        public void RelayMessage(CChatRoom_IncomingChatMessage_Notification notification) {
            if(PipeStream != null && PipeStream.IsConnected) {
                string message = $"{{ 'sender': {notification.steamid_sender}, 'message': '{notification.message}' }}";
                try {
                    
                    PipeStream.Write(encoding.GetBytes(message));
                    
                } catch (Exception e) {
                    Debug.Write(e);
                }
                
            }

            //throw new NotImplementedException();
        }

        //public class PipeClient {
        //    private static int numClients = 4;

        //    public static void Main(string[] args) {
        //        if (args.Length > 0) {
        //            if (args[0] == "spawnclient") {

        //            }
        //        } else {
        //            Console.WriteLine("\n*** Named pipe client stream with impersonation example ***\n");
        //            StartClients();
        //        }
        //    }

        //    // Helper function to create pipe client processes
        //    private static void StartClients() {
        //        string currentProcessName = Environment.CommandLine;
        //        currentProcessName = Path.ChangeExtension(currentProcessName, ".exe");
        //        Process[] plist = new Process[numClients];

        //        Console.WriteLine("Spawning client processes...\n");

        //        if (currentProcessName.Contains(Environment.CurrentDirectory)) {
        //            currentProcessName = currentProcessName.Replace(Environment.CurrentDirectory, String.Empty);
        //        }

        //        // Remove extra characters when launched from Visual Studio
        //        currentProcessName = currentProcessName.Replace("\\", String.Empty);
        //        currentProcessName = currentProcessName.Replace("\"", String.Empty);

        //        int i;
        //        for (i = 0; i < numClients; i++) {
        //            // Start 'this' program but spawn a named pipe client.
        //            plist[i] = Process.Start(currentProcessName, "spawnclient");
        //        }
        //        while (i > 0) {
        //            for (int j = 0; j < numClients; j++) {
        //                if (plist[j] != null) {
        //                    if (plist[j].HasExited) {
        //                        Console.WriteLine($"Client process[{plist[j].Id}] has exited.");
        //                        plist[j] = null;
        //                        i--;    // decrement the process watch count
        //                    } else {
        //                        Thread.Sleep(250);
        //                    }
        //                }
        //            }
        //        }
        //        Console.WriteLine("\nClient processes finished, exiting.");
        //    }
        //}

    }



    //// https://docs.microsoft.com/en-us/dotnet/standard/io/how-to-use-named-pipes-for-network-interprocess-communication
    //// Defines the data protocol for reading and writing strings on our stream.
    //public class StreamString {
    //    private Stream ioStream;
    //    private UnicodeEncoding streamEncoding;

    //    public StreamString(Stream ioStream) {
    //        this.ioStream = ioStream;
    //        streamEncoding = new UnicodeEncoding();
    //    }

    //    public string ReadString() {
    //        int len;
    //        len = ioStream.ReadByte() * 256;
    //        len += ioStream.ReadByte();
    //        var inBuffer = new byte[len];
    //        ioStream.Read(inBuffer, 0, len);

    //        return streamEncoding.GetString(inBuffer);
    //    }

    //    public int WriteString(string outString) {
    //        byte[] outBuffer = streamEncoding.GetBytes(outString);
    //        int len = outBuffer.Length;
    //        if (len > UInt16.MaxValue) {
    //            len = (int)UInt16.MaxValue;
    //        }
    //        ioStream.WriteByte((byte)(len / 256));
    //        ioStream.WriteByte((byte)(len & 255));
    //        ioStream.Write(outBuffer, 0, len);
    //        ioStream.Flush();

    //        return outBuffer.Length + 2;
    //    }
    //}
}
