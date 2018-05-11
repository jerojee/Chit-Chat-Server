using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;
using ChitChat.Events;

namespace CC_Server
{
    class ChatRoom
    {
        private string            _name;
        private List<Connection> _roomMembers;

        public string           Name        { get => _name;        set => _name        = value; }
        public List<Connection> RoomMembers { get => _roomMembers; set => _roomMembers = value; }

        public ChatRoom(string name)
        {
            _name = name;
            _roomMembers = new List<Connection>();

            G.g_ChatRooms.Add(this);

            Console.WriteLine($"Created chatroom with name: {name}");
        }

        public void BroadcastMessage(pbSendMessage message)
        {
            var outMessage = new pbReceiveMessage
            {
                Type = EventType.RECV_MESSAGE,
                InMessage = message.OutMessage,
                SenderName = message.SenderName,
                RoomName = message.RoomName
            };

            foreach(var member in RoomMembers)
            {
                if (member._userInfo.UserName != message.SenderName)
                {
                    Serializer.SerializeWithLengthPrefix(member._clientStream, outMessage, PrefixStyle.Base128);
                }
            }
        }

        public void PrintMembers()
        {
            Console.WriteLine($"-----USERS IN {Name}-----");

            foreach(var member in RoomMembers)
            {
                Console.WriteLine(member._userInfo.UserName);
            }

            Console.WriteLine("---------------------------");
        }

    }
}
