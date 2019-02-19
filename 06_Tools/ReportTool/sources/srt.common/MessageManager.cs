using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace srt.common
{
    /// <summary>
    /// For communication with the outside world
    /// </summary>
    public static class MessageManager
    {

        /// <summary>
        /// Publish logmessage with reason to skip processing (in log and report)
        /// </summary>
        /// <param name="moduleName"></param>
        /// <param name="message"></param>
        public static void LogSkipMessage(string moduleName, string message)
        {
            Log.WriteLine(string.Format("SKIP {0}: {1}", moduleName, message));
        }
    }
}
