﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;

namespace ChitChat.Events
{
    public enum EventType
    {
        CREATE_ACCT,
        LOGIN,
        LOGOUT,
        USER_INFO,
        SERVER_REPLY,
        CREATE_CHAT, 
        JOIN_CHAT,
        SEND_MESSAGE, 
        RECV_MESSAGE,
        ADD_FRIEND,
        INITIAL_INFO
    }

    [ProtoContract]
    [ProtoInclude(100, typeof(pbUserInfo))]
    [ProtoInclude(101, typeof(pbLoginRequest))]
    [ProtoInclude(102, typeof(pbCreateAccountRequest))]
    [ProtoInclude(104, typeof(pbCreateChatRequest))]
    [ProtoInclude(105, typeof(pbSendMessage))]
    [ProtoInclude(106, typeof(pbReceiveMessage))]
    [ProtoInclude(108, typeof(pbJoinChatRequest))]
    [ProtoInclude(109, typeof(pbResponse))]
    [ProtoInclude(110, typeof(pbLogoutRequest))]
    [ProtoInclude(111, typeof(pbAddFriendRequest))]
    [ProtoInclude(112, typeof(pbInitialClientInfo))]
    public class BaseRequest
    {
        [ProtoMember(1)]
        public EventType Type { get; set; }
    }

    [ProtoContract]
    public class pbUserInfo : BaseRequest
    {
        [ProtoMember(1)]
        public string UserName { get; set; }

        [ProtoMember(2)]
        public string Password { get; set; }
    }

    [ProtoContract]
    public class pbLoginRequest : BaseRequest
    {
        [ProtoMember(1)]
        public pbUserInfo userInfo { get; set; }
    }

    [ProtoContract]
    public class pbCreateAccountRequest : BaseRequest
    {
        [ProtoMember(1)]
        public pbUserInfo userInfo { get; set; }
    }

    [ProtoContract]
    public class pbCreateChatRequest : BaseRequest
    {
        [ProtoMember(1)]
        public string ChatName { get; set; }

        //Possibly add member for a list of users?
    }

    [ProtoContract]
    public class pbJoinChatRequest : BaseRequest
    {
        [ProtoMember(1)]
        public string ChatName { get; set; }
    }

    [ProtoContract]
    public class pbResponse : BaseRequest
    {
        [ProtoMember(1)]
        public bool requestStatus { get; set; }
    }

    [ProtoContract]
    public class pbSendMessage : BaseRequest
    {
        [ProtoMember(1)]
        public string SenderName { get; set; }

        [ProtoMember(2)]
        public string OutMessage { get; set; }

        [ProtoMember(3)]
        public string RoomName { get; set; }
    }

    [ProtoContract]
    public class pbReceiveMessage : BaseRequest
    {
        [ProtoMember(1)]
        public string SenderName { get; set; }

        [ProtoMember(2)]
        public string InMessage { get; set; }

        [ProtoMember(3)]
        public string RoomName { get; set; }
    }

    [ProtoContract]
    public class pbLogoutRequest : BaseRequest
    {
        //LOGOUT REQUEST HAS ONLY AN EVENT TYPE
    }

    [ProtoContract]
    public class pbAddFriendRequest : BaseRequest
    {
        [ProtoMember(1)]
        public string FriendName { get; set; }

        [ProtoMember(2)]
        public string UserName { get; set; }
    }

    [ProtoContract]
    public class pbInitialClientInfo : BaseRequest
    {
        [ProtoMember(1)]
        public List<string> FriendList { get; set; }
    }
}

