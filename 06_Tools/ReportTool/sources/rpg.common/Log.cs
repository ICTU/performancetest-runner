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
        private static DateTime prevTimestamp = DateTime.Now;

        private static DateTime GetAndResetPrev()
        {
            DateTime result = prevTimestamp;
            prevTimestamp = DateTime.Now;
            return result;
        }

        /// <summary>
        /// Get the appropriate timestamp for logging
        /// </summary>
        /// <returns></returns>
        public static string GetTimestamp()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " (+" + (DateTime.Now - GetAndResetPrev()).TotalSeconds.ToString("0.000") + "s)";
        }

        /// <summary>
        /// Write a log line (with line separator)
        /// </summary>
        /// <param name="message"></param>
        public static void WriteLine(string message, bool printTimestamp = false)
        {
            Console.WriteLine(printTimestamp ? GetTimestamp() + " " + message : message);
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
