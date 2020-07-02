using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace UsdzSharpie
{
    public static class Logger
    {
        public static StreamWriter LogFile; 

        public static void LogLine(string message)
        {
            if (LogFile != null)
            {
                LogFile.WriteLine(message);
            }
            Console.WriteLine(message);
        }
    }
}
