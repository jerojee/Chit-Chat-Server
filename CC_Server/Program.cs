using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ProtoBuf;
using ChitChat.Events;

namespace CC_Server
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World! \n");

            Server server = new Server();

            // Server's start and listen thread should be running as long as the server
            // is active, so create the task as a LongRunning task
            G.g_Tasks.Add(Task.Factory.StartNew(() => server.StartAndListen()
                          , TaskCreationOptions.LongRunning));

            // Keep programming running forever, or until a key is pressed
            while (G.g_IsRunning)
            {
                if (Console.KeyAvailable)
                {
                    G.g_IsRunning = false;
                    break;
                }

                Console.Write(". ");

                Thread.Sleep(1000);
            }

            // Should wait for all threads to finish and clean up before exiting
            Task.WaitAll(G.g_Tasks.ToArray());
        }
    }
}