using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using SteamKit2;
using SteamKit2.Internal;
using SteamKit2.Unified.Internal;


//
// Sample 5: SteamGuard
//
// this sample goes into detail for how to handle steamguard protected accounts and how to login to them
//
// SteamGuard works by enforcing a two factor authentication scheme
// upon first logon to an account with SG enabled, the steam server will email an authcode to the validated address of the account
// this authcode token can be used as the second factor during logon, but the token has a limited time span in which it is valid
//
// after a client logs on using the authcode, the steam server will generate a blob of random data that the client stores called a "sentry file"
// this sentry file is then used in all subsequent logons as the second factor
// ownership of this file provides proof that the machine being used to logon is owned by the client in question
//
// the usual login flow is thus:
// 1. connect to the server
// 2. logon to account with only username and password
// at this point, if the account is steamguard protected, the LoggedOnCallback will have a result of AccountLogonDenied
// the server will disconnect the client and email the authcode
//
// the login flow must then be restarted:
// 1. connect to server
// 2. logon to account using username, password, and authcode
// at this point, login wil succeed and a UpdateMachineAuthCallback callback will be posted with the sentry file data from the steam server
// the client will save the file, and reply to the server informing that it has accepted the sentry file
// 
// all subsequent logons will use this flow:
// 1. connect to server
// 2. logon to account using username, password, and sha-1 hash of the sentry file


namespace SteamChat {

    class Program
    {
        static SteamClient steamClient;
        static CallbackManager manager;

        static SteamUser steamUser;

        static SteamUnifiedMessages steamUnifiedMessages;
        static SteamUnifiedMessages.UnifiedService<IPlayer> playerService;
        static SteamUnifiedMessages.UnifiedService<IChatRoom> chatRoomService;
        static SteamUnifiedMessages.UnifiedService<IChatRoomClient> chatRoomClientService;

        static PipeHandler pipeHandler = new PipeHandler();
        

        static bool isRunning;

        static string user, pass;
        static string authCode, twoFactorAuth;

        static ulong myGroupId = 7643625;
        static ulong defualtChatId = 24932502;

        static void Main(string[] args) {
            if (args.Length < 2) {
                Console.WriteLine("Sample5: No username and password specified!");
                return;
            }

            // save our logon details
            user = args[0];
            pass = args[1];

            // create our steamclient instance
            steamClient = new SteamClient();


            // create the callback manager which will route callbacks to function calls
            manager = new CallbackManager(steamClient);

            // get the steamuser handler, which is used for logging on after successfully connecting
            steamUser = steamClient.GetHandler<SteamUser>();


            // get the steam unified messages handler, which is used for sending and receiving responses from the unified service api
            steamUnifiedMessages = steamClient.GetHandler<SteamUnifiedMessages>();

            // we also want to create our local service interface, which will help us build requests to the unified api
            playerService = steamUnifiedMessages.CreateService<IPlayer>();
            chatRoomService = steamUnifiedMessages.CreateService<IChatRoom>();
            chatRoomClientService = steamUnifiedMessages.CreateService<IChatRoomClient>();


            // register a few callbacks we're interested in
            // these are registered upon creation to a callback manager, which will then route the callbacks
            // to the functions specified
            manager.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
            manager.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);

            manager.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOn);
            manager.Subscribe<SteamUser.LoggedOffCallback>(OnLoggedOff);

            // this callback is triggered when the steam servers wish for the client to store the sentry file
            manager.Subscribe<SteamUser.UpdateMachineAuthCallback>(OnMachineAuth);


            // we use the following callbacks for unified service responses
            manager.Subscribe<SteamUnifiedMessages.ServiceMethodResponse>(OnMethodResponse);
            manager.Subscribe<SteamUnifiedMessages.ServiceMethodNotification>(OnServiceMethod);

            


            //manager.Subscribe<SteamKit2.SteamFriends.ChatRoomInfoCallback>(OnChatRoomInfoCallback);
            //SteamKit2.SteamFriends.ChatRoomInfoCallback();

            //manager.Subscribe<CChatRoom_IncomingChatMessage_Notification>(OnIncomingChatMessage_Notification);


            isRunning = true;

            Console.WriteLine("Connecting to Steam...");

            // initiate the connection
            steamClient.Connect();

            // create our callback handling loop
            while (isRunning) {
                // in order for the callbacks to get routed, they need to be handled by the manager
                manager.RunWaitCallbacks(TimeSpan.FromSeconds(1));
            }
        }

        private static void OnChatRoomInfoCallback(SteamFriends.ChatRoomInfoCallback obj) {
            
        }

        static void OnIncomingChatMessage_Notification(CChatRoom_IncomingChatMessage_Notification obj) {

        }

        static void OnConnected(SteamClient.ConnectedCallback callback) {
            Console.WriteLine("Connected to Steam! Logging in '{0}'...", user);

            byte[] sentryHash = null;
            if (File.Exists("sentry.bin")) {
                // if we have a saved sentry file, read and sha-1 hash it
                byte[] sentryFile = File.ReadAllBytes("sentry.bin");
                sentryHash = CryptoHelper.SHAHash(sentryFile);
            }

            steamUser.LogOn(new SteamUser.LogOnDetails {
                Username = user,
                Password = pass,

                // in this sample, we pass in an additional authcode
                // this value will be null (which is the default) for our first logon attempt
                AuthCode = authCode,

                // if the account is using 2-factor auth, we'll provide the two factor code instead
                // this will also be null on our first logon attempt
                TwoFactorCode = twoFactorAuth,

                // our subsequent logons use the hash of the sentry file as proof of ownership of the file
                // this will also be null for our first (no authcode) and second (authcode only) logon attempts
                SentryFileHash = sentryHash,
            });
        }

        static void OnDisconnected(SteamClient.DisconnectedCallback callback) {
            // after recieving an AccountLogonDenied, we'll be disconnected from steam
            // so after we read an authcode from the user, we need to reconnect to begin the logon flow again

            Console.WriteLine("Disconnected from Steam, reconnecting in 5...");

            Thread.Sleep(TimeSpan.FromSeconds(5));

            steamClient.Connect();
        }

        static void OnLoggedOn(SteamUser.LoggedOnCallback callback) {
            bool isSteamGuard = callback.Result == EResult.AccountLogonDenied;
            bool is2FA = callback.Result == EResult.AccountLoginDeniedNeedTwoFactor;

            if (isSteamGuard || is2FA) {
                Console.WriteLine("This account is SteamGuard protected!");

                if (is2FA) {
                    Console.Write("Please enter your 2 factor auth code from your authenticator app: ");
                    twoFactorAuth = Console.ReadLine();
                } else {
                    Console.Write("Please enter the auth code sent to the email at {0}: ", callback.EmailDomain);
                    authCode = Console.ReadLine();
                }

                return;
            }

            if (callback.Result != EResult.OK) {
                Console.WriteLine("Unable to logon to Steam: {0} / {1}", callback.Result, callback.ExtendedResult);

                isRunning = false;
                return;
            }

            Console.WriteLine("Successfully logged on!");

            // at this point, we'd be able to perform actions on Steam

            OnLoggedOn();
        }

        static void OnLoggedOff(SteamUser.LoggedOffCallback callback) {
            Console.WriteLine("Logged off of Steam: {0}", callback.Result);
        }

        static void OnMachineAuth(SteamUser.UpdateMachineAuthCallback callback) {
            Console.WriteLine("Updating sentryfile...");

            // write out our sentry file
            // ideally we'd want to write to the filename specified in the callback
            // but then this sample would require more code to find the correct sentry file to read during logon
            // for the sake of simplicity, we'll just use "sentry.bin"

            int fileSize;
            byte[] sentryHash;
            using (var fs = File.Open("sentry.bin", FileMode.OpenOrCreate, FileAccess.ReadWrite)) {
                fs.Seek(callback.Offset, SeekOrigin.Begin);
                fs.Write(callback.Data, 0, callback.BytesToWrite);
                fileSize = (int)fs.Length;

                fs.Seek(0, SeekOrigin.Begin);
                using (var sha = SHA1.Create()) {
                    sentryHash = sha.ComputeHash(fs);
                }
            }

            // inform the steam servers that we're accepting this sentry file
            steamUser.SendMachineAuthResponse(new SteamUser.MachineAuthDetails {
                JobID = callback.JobID,

                FileName = callback.FileName,

                BytesWritten = callback.BytesToWrite,
                FileSize = fileSize,
                Offset = callback.Offset,

                Result = EResult.OK,
                LastError = 0,

                OneTimePassword = callback.OneTimePassword,

                SentryFileHash = sentryHash,
            });

            Console.WriteLine("Done!");
        }


        static void OnLoggedOn() {

            // now that we're logged onto Steam, lets query the IPlayer service for our badge levels

            // first, build our request object, these are autogenerated and can normally be found in the SteamKit2.Unified.Internal namespace
            //CPlayer_GetGameBadgeLevels_Request req = new CPlayer_GetGameBadgeLevels_Request {
            //    // we want to know our 440 (TF2) badge level
            //    appid = 440,
            //};

            bool send_chat = true;
            if (send_chat) {

                {
                    // ha ezt küldöd, akkor fogsz notificationoket kapni
                    uint chatMode = 2;
                    ClientMsgProtobuf<CMsgClientUIMode> request = new ClientMsgProtobuf<CMsgClientUIMode>(EMsg.ClientCurrentUIMode) { Body = { chat_mode = chatMode } };
                    steamClient.Send(request);
                }
                


                CChatRoom_SendChatMessage_Request req = new CChatRoom_SendChatMessage_Request {
                    message = "Bot logged in.",
                    messageSpecified = true,
                    chat_group_id = myGroupId,
                    chat_group_idSpecified = true,
                    chat_id = defualtChatId,
                    chat_idSpecified = true
                };
                chatRoomService.SendMessage(x => x.SendChatMessage(req));

            } else {

                CChatRoom_GetMyChatRoomGroups_Request req = new CChatRoom_GetMyChatRoomGroups_Request {
                    // empty message
                };


                // now lets send the request, this is done by building an expression tree with the IPlayer interface
                myJobID = chatRoomService.SendMessage(x => x.GetMyChatRoomGroups(req));

            }
            


            // alternatively, the request can be made using SteamUnifiedMessages directly, but then you must build the service request name manually
            // the name format is in the form of <Service>.<Method>#<Version>
            //steamUnifiedMessages.SendMessage("Player.GetGameBadgeLevels#1", req);
        }

        static JobID myJobID = JobID.Invalid;


        static void ProcessMyJob(SteamUnifiedMessages.ServiceMethodResponse callback) {

            // and check for success
            if (callback.Result != EResult.OK) {
                Console.WriteLine($"Unified service request failed with {callback.Result}");
                return;
            }

            // retrieve the deserialized response for the request we made
            // notice the naming pattern
            // for requests: CMyService_Method_Request
            // for responses: CMyService_Method_Response
            //CPlayer_GetGameBadgeLevels_Response resp = callback.GetDeserializedResponse<CPlayer_GetGameBadgeLevels_Response>();
            CChatRoom_GetMyChatRoomGroups_Response resp = callback.GetDeserializedResponse<CChatRoom_GetMyChatRoomGroups_Response>();


            //Console.WriteLine($"Our player level is {resp.}");

            foreach (var chat_room_group in resp.chat_room_groups) {
                
                Console.WriteLine($"id : {chat_room_group.user_chat_group_state.chat_group_id}, name: {chat_room_group.group_summary.chat_group_name}, default_chat_id: {chat_room_group.group_summary.default_chat_id}");
            }

            myJobID = JobID.Invalid;

            // now that we've completed our task, lets log off
            //steamUser.LogOff();
        }

        static void OnMethodResponse(SteamUnifiedMessages.ServiceMethodResponse callback) {


            if (callback.JobID == myJobID) {
                // always double check the jobid of the response to ensure you're matching to your original request
                ProcessMyJob(callback);
                return;
            }

            if(callback.ServiceName == "ChatRoomClient"  && callback.RpcName == "NotifyIncomingChatMessage") {
                CChatRoom_IncomingChatMessage_Notification incomingChatMessage = callback.GetDeserializedResponse<CChatRoom_IncomingChatMessage_Notification>();
                Console.WriteLine($"from: {incomingChatMessage.steamid_sender}\n message : {incomingChatMessage.message} \n");

            }
            

        }

        private static async Task SendMessageToGroupChat(string message) {

            CChatRoom_SendChatMessage_Request request = new CChatRoom_SendChatMessage_Request {
                chat_group_id = myGroupId,
                chat_id = defualtChatId,
                message = message
            };

            await chatRoomService.SendMessage(x => x.SendChatMessage(request));
        }

        private static async Task OnIncomingChatMessage(CChatRoom_IncomingChatMessage_Notification notification) {

            Console.WriteLine($"from: {notification.steamid_sender}\n message : {notification.message} \n");

            if (notification.message == "ping") {
                CChatRoom_SendChatMessage_Request request = new CChatRoom_SendChatMessage_Request {
                    chat_group_id = notification.chat_group_id,
                    chat_id = notification.chat_id,
                    message = "pong"
                };

                await chatRoomService.SendMessage(x => x.SendChatMessage(request));
            }

            pipeHandler.RelayMessage(notification);
        }

        private static async void OnServiceMethod(SteamUnifiedMessages.ServiceMethodNotification notification) {
            switch (notification.MethodName) {
                case "ChatRoomClient.NotifyIncomingChatMessage#1":
                    await OnIncomingChatMessage((CChatRoom_IncomingChatMessage_Notification)notification.Body);

                    break;
            }
        }


    }
}
