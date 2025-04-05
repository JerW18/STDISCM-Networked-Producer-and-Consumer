using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P3___Networked_Producer
{
    internal class Logger
    {
        public static Action<string>? LogAction { get; set; }

        public static void Log(string message)
        {
            Debug.WriteLine(message);
            LogAction?.Invoke(message);
        }
    }
}
