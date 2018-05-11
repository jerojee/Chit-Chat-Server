using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CC_Server
{
    public class Singleton
    {
        private static Singleton _instance;

        protected Singleton() { }

        public static Singleton Instance
        {
            get
            {
                if (Instance == null)
                {
                    _instance = new Singleton();
                }

                return _instance;
            }
        }
    }
}
