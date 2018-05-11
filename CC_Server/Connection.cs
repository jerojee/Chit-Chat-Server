using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using ProtoBuf;
using ChitChat.Events;

namespace CC_Server
{
    public class Connection
    {
        public  pbUserInfo       _userInfo         { get; set; }
        public  TcpClient        _client           { get; set; }
        public  NetworkStream    _clientStream     { get; set; }
        public  List<string>     _friendList;      
        private List<ChatRoom>   _clientChatRooms;

        public Connection()
        {
            _client         = null;
            _userInfo       = null;
            _clientStream   = null;
            _friendList     = new List<string>();
        }

        public Connection(TcpClient client)
        {
            _userInfo        = null;
            _client          = client;
            _clientStream    = client.GetStream();
            _clientChatRooms = new List<ChatRoom>();
            _friendList      = new List<string>();
        }

        public void HandleClientRequests()
        {
            bool done = false;
            Console.WriteLine("\nClient has started!");

            // Handle all client requests here
            while (G.g_IsRunning && !done)
            {
                // The server can't know what type of request is coming in until it's already been read
                // so read in a generic message, then determine the type
                var incomingRequest = new BaseRequest();

                try
                {
                    incomingRequest = Serializer.DeserializeWithLengthPrefix<BaseRequest>(_clientStream, PrefixStyle.Base128);
                }
                catch (SocketException se)
                {
                    Console.WriteLine(se.ToString());
                }

                Console.WriteLine($"\nReceived message of type: {incomingRequest.Type.ToString()}");

                // Determine the type of the request, then pass the request off to the appropriate manager
                switch (incomingRequest.Type)
                {
                    case EventType.CREATE_ACCT:
                        CreateAccount((pbCreateAccountRequest)incomingRequest);
                        break;

                    case EventType.LOGIN:
                        Login((pbLoginRequest)incomingRequest);
                        break;

                    case EventType.LOGOUT:
                        Logout();
                        done = true;
                        break;

                    case EventType.SEND_MESSAGE:
                        SendMessage((pbSendMessage)incomingRequest);
                        break;

                    case EventType.USER_INFO:
                        this._userInfo = (pbUserInfo)incomingRequest;
                        break;

                    case EventType.CREATE_CHAT:
                        CreateChatRoom((pbCreateChatRequest)incomingRequest);
                        break;

                    case EventType.JOIN_CHAT:
                        JoinChatRoom((pbJoinChatRequest)incomingRequest);
                        break;

                    case EventType.ADD_FRIEND:
                        AddFriend((pbAddFriendRequest)incomingRequest);
                        break;
                }
            }
        }

        public void Login(pbLoginRequest loginRequest)
        {
            bool loginStatus = AccountManager.Instance.Login(loginRequest);

            var loginReply = new pbResponse()
            {
                Type = EventType.SERVER_REPLY,
                requestStatus = loginStatus
            };

            Serializer.SerializeWithLengthPrefix(_clientStream, loginReply, PrefixStyle.Base128);

            // If the log in was a success, then the user can get added to the global Connection list,
            // and also send the user any info it might need upon logging in (friends list, etc.)
            // This is to avoid having a huge list of non-logged in users
            if (loginStatus)
            {
                Console.WriteLine("Login was successful");
                _userInfo = loginRequest.userInfo;

                ConnectionManager.Instance.AddConnection(this);

                _friendList = G.g_database.GetFriendsListFromDB(this._userInfo.UserName);

                var initialClientInfo = new pbInitialClientInfo()
                {
                    Type = EventType.INITIAL_INFO,
                    FriendList = _friendList
                };

                Serializer.SerializeWithLengthPrefix(_clientStream, initialClientInfo, PrefixStyle.Base128);
            }
            else
            {
                Console.WriteLine("Login failed");
                return;
            }
        }

        public void AddFriend(pbAddFriendRequest addFriendRequest)
        {
            if (AccountManager.Instance.AddFriend(addFriendRequest, ref this._friendList))
            {
                Console.WriteLine("Successfully added friend");
            }

        }

        public void Logout()
        {
            Console.WriteLine($"{this._userInfo.UserName} has logged out");
            CloseConnection();
        }
        
        public void CloseConnection()
        {
            ConnectionManager.Instance.Connections.Remove(this);
            //this._clientStream.Close();
            //this._client.Close();
        }

        public void CreateAccount(pbCreateAccountRequest createAccountRequest)
        {
            bool createAccountStatus = AccountManager.Instance.CreateAccount(createAccountRequest);

            var createAccountReply = new pbResponse()
            {
                Type = EventType.SERVER_REPLY,
                requestStatus = createAccountStatus
            };

            Serializer.SerializeWithLengthPrefix(_clientStream, createAccountReply, PrefixStyle.Base128);


            if (createAccountStatus)
            {
                Console.WriteLine("Account creation was successful");
            }
            else
            {
                Console.WriteLine("Account creation failed");
            }

        }

        public void SendMessage(pbSendMessage outMessage)
        {
            var receivingChatRoom = _clientChatRooms.Find(chatroom => chatroom.Name.Contains(outMessage.RoomName));

            if (receivingChatRoom != null)
            {
                Console.WriteLine("Found room");
                receivingChatRoom.BroadcastMessage(outMessage);
                Console.WriteLine("Message was sent");
            }
        }

        public void CreateChatRoom(pbCreateChatRequest createChatRequest)
        {
            Console.WriteLine("Inside create chat");

            bool createChatStatus = false;

            // Don't let users create chat rooms with the same name
            if (!G.g_ChatRooms.Exists(chatroom => chatroom.Name.Equals(createChatRequest.ChatName)))
            {
                var newRoom = new ChatRoom(createChatRequest.ChatName);

                if (newRoom != null)
                {
                    _clientChatRooms.Add(newRoom);
                    newRoom.RoomMembers.Add(this);

                    createChatStatus = true;
                }
            }
            else
            {
                Console.WriteLine("Error creating chat room");
            }
            var createChatResponse = new pbResponse()
            {
                Type = EventType.SERVER_REPLY,
                requestStatus = createChatStatus
            };

            //Serializer.SerializeWithLengthPrefix(_clientStream, createChatResponse, PrefixStyle.Base128);
        }

        public void JoinChatRoom(pbJoinChatRequest joinChatRequest)
        {
            bool joinWasSuccess = false;

            Console.WriteLine("Inside join chat");

            var chatRoomToJoin = G.g_ChatRooms.Find(chatroom =>
                                 chatroom.Name.Contains(joinChatRequest.ChatName));
            
            if (chatRoomToJoin != null)
            {
                this._clientChatRooms.Add(chatRoomToJoin);

                chatRoomToJoin.RoomMembers.Add(this);

                joinWasSuccess = true;

                Console.WriteLine($"{_userInfo.UserName} has joined the chat {joinChatRequest.ChatName}");

                var joinAnnouncement = new pbSendMessage()
                {
                    Type = EventType.SEND_MESSAGE,
                    SenderName = "SERVER",
                    OutMessage = $"{this._userInfo.UserName} has joined the chat",
                    RoomName = joinChatRequest.ChatName
                };

                SendMessage(joinAnnouncement);
            }

            var joinChatResponse = new pbResponse()
            {
                Type = EventType.SERVER_REPLY,
                requestStatus = joinWasSuccess
            };

            //Serializer.SerializeWithLengthPrefix(_clientStream, joinChatResponse, PrefixStyle.Base128);
        }

    }
}

