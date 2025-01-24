using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClusteredTimerApp
{
    public static class ConsoleExtensions
    {
        public static void Log(string message)
        {
            //int threadId = Thread.CurrentThread.ManagedThreadId;
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");
        }
    }
}
