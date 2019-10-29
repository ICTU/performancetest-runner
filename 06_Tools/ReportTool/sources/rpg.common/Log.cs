using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace rpg.common
{
    /// <summary>
    /// Generic loggic (singleton)
    /// </summary>
    public class Log
    {
        /// <summary>
        /// Get the appropriate timestamp for logging
        /// </summary>
        /// <returns></returns>
        public static string GetTimestamp()
        {
            //return DateTime.Now.ToString("s");  MM/dd/yy H:mm:ss zzz
            return DateTime.Now.ToString("[yyMMddTHHmmss]");
        }

        /// <summary>
        /// Write a log line (with line separator)
        /// </summary>
        /// <param name="message"></param>
        public static void WriteLine(String message)
        {
            //Console.WriteLine(GetTimestamp()+" "+message);
            Console.WriteLine(message);
        }

        /// <summary>
        /// Next-line
        /// </summary>
        public static void WriteLine()
        {
            WriteLine("");
        }

        /// <summary>
        /// Write a log line (without line separator)
        /// </summary>
        /// <param name="message"></param>
        public static void Write(String message)
        {
            //Console.Write(GetTimestamp() + " " + message);
            Console.Write(message);
        }
    }
}
