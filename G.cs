sing System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CC_Server
{
    public static class G
    {
        public static bool g_IsRunning = true;
        public static List<Task> g_Tasks = new List<Task>();
    }
}
