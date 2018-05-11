using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CC_Server
{
    internal class G
    {
        public static bool            g_IsRunning = true;
        public static List<Task>      g_Tasks     = new List<Task>();
        public static List<ChatRoom>  g_ChatRooms = new List<ChatRoom>();
        public static DatabaseManager g_database = new DatabaseManager();
    }
}