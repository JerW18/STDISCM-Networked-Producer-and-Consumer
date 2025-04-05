using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P3___Networked_Consumer
{
    public static class Logger
    {
        public static Action<string>? LogMsg;

        public static void Log(string message)
        {
            Debug.WriteLine(message); 
            LogMsg?.Invoke(message);
        }
    }

}
