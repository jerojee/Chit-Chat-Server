using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace CC_Server
{
    public sealed class ConnectionManager
    {
        #region Singleton
        private static readonly Object _lock = new Object();
        private static ConnectionManager _instance;

        private ConnectionManager()
        {
            _connections = new List<Connection>();
        }

        public static ConnectionManager Instance
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
                        _instance = new ConnectionManager();
                    }
                }
                return _instance;
            }
        }

        #endregion

        private List<Connection> _connections;
        public  List<Connection> Connections  { get => _connections; set => _connections = value; }

        public void AddConnection(Connection newConnection)
        {
            Connections.Add(newConnection);
        }

        public bool FindUserByName(string username)
        {
            Console.WriteLine($"looking for user: {username}");

            // Find a user based on the username parameter. This will only search for logged in users,
            // so make sure that the userInfo isn't null first
            bool foundUser = Connections.Exists(user => (user._userInfo != null)
            && (user._userInfo.UserName.Equals(username)));

            if(foundUser)
            {
                return true;
            }

            return false;
        }

        public void PrintConnectedUsers()
        {
            Console.WriteLine("----- CONNECTED USERS -----");
            foreach(var connection in Connections)
            {
                Console.WriteLine($"{connection._userInfo.UserName}");
            }
            Console.WriteLine("--------------------------");
        }
    }
}


// Find new chat room object
// Hide current text box mainTextBox.Hide()
// mainTextBox = newRoom.textBox
// show mainTextBox.Show()