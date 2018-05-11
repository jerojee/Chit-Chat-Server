using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using ProtoBuf;
using ChitChat.Events;

namespace CC_Server
{
    class Server
    {
        private TcpListener  _listener;
        private int          _port;

        public Server()
        {
            _port = 27015;
            _listener = new TcpListener(IPAddress.IPv6Any, _port);
            _listener.Server.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
        }

        public async Task StartAndListen()
        {
            if (G.g_database.OpenConnection())
            {
                Console.WriteLine("\nDB WAS CONNECTED SUCCESSFULLY");
            }

            _listener.Start();

            Console.WriteLine("\nStarting listening.");

            try
            {
                while (G.g_IsRunning)
                {
                    var newClient = (await _listener.AcceptTcpClientAsync());

                    Console.WriteLine("\nAccepted a connection");

                    var newConnection = new Connection(newClient);

                    G.g_Tasks.Add(Task.Factory.StartNew(() => newConnection.HandleClientRequests(),
                     TaskCreationOptions.LongRunning));
                }
            }
            catch(SocketException soe)
            {
                Console.WriteLine("Socket exception came from Server.cs");
                Console.WriteLine(soe.ToString());
            }
            finally
            {
                _listener?.Stop();
                G.g_database.CloseConnection();
            }
        }
    }
}
