using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data;
using System.Data.SQLite;
using System.Net.Sockets;
using ProtoBuf;
using ChitChat.Events;

namespace CC_Server
{
    public sealed class AccountManager
    {
        #region Singleton
        private static readonly Object          _lock = new Object();
        private static          AccountManager  _instance;

        private AccountManager() { }

        public static AccountManager Instance
        {
            get
            {
                if (_instance != null)
                {
                    return _instance;
                }

                lock (_lock)
                {

                    if (_instance == null)
                    {
                        _instance = new AccountManager();
                    }
                }
                return _instance;
            }
        }

        #endregion

        private readonly Object _databaseLock = new Object();

        public bool CreateAccount(pbCreateAccountRequest createAccountRequest)
        {
            bool createWasSuccess = false;

            Console.WriteLine("\nCreating account...");

            if (createAccountRequest != null)
            {
                // Ensure that users get inserted to the database safely
                lock (_databaseLock)
                {
                    if (G.g_database.InsertNewUser(createAccountRequest.userInfo) >= 0)
                    {
                        createWasSuccess = true;
                    }
                }
            }

            return createWasSuccess;
        }

        public bool Login (pbLoginRequest loginRequest)
        {
            bool loginWasSuccess = false;

            Console.WriteLine($"\nLogging in user: {loginRequest.userInfo.UserName}");

            if (loginRequest != null)
            {
                // Before a user can log in, make sure that the user isn't already logged in elsewhere
                // to avoid one user being logged in on multiple machines
                if(ConnectionManager.Instance.FindUserByName(loginRequest.userInfo.UserName))
                {
                    Console.WriteLine("A user was found to be logged in with this info");
                    return false;
                }

                // Ensure that the database gets read safely 
                lock (_databaseLock)
                {
                    loginWasSuccess = G.g_database.CheckUserLogin(loginRequest.userInfo.UserName,
                        loginRequest.userInfo.Password);
                }
            }

            return loginWasSuccess;
        }

        public bool AddFriend(pbAddFriendRequest addFriendRequest, ref List<string> friendList)
        {
            if (addFriendRequest != null)
            {
                if ((addFriendRequest.FriendName == null) || (addFriendRequest.FriendName.Length <= 0))
                {
                    Console.WriteLine("Got a null user name");
                }

                lock (_databaseLock)
                {
                    if (G.g_database.InsertNewFriend(addFriendRequest.UserName, addFriendRequest.FriendName) >= 0)
                    {
                        Console.WriteLine($"Entered {addFriendRequest.FriendName} to {addFriendRequest.UserName}'s friends list.");
                        friendList.Add(addFriendRequest.FriendName);
                    }
                    else
                    {
                        Console.WriteLine("Add friend failed");
                    }
                }

                return true;
            }

            return false;
        }
    }
}
