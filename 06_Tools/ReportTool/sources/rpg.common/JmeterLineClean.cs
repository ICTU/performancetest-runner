using System.Text.RegularExpressions;
using System.Collections.Generic;
using rpg.common;

namespace rpg.common
{
    /// <summary>
    /// Handle cleaned up jtl line (parsed by JmeterLineRaw)
    /// </summary>
    public class JmeterLineClean
    {
        /// <summary>
        /// substring position: lb=0; ts=1; t=2; s=3; na=4
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public static Dictionary<string, string> Parse(string line)
        {
            //Log.WriteLine("parse clean log line :"+line);

            Dictionary<string, string> result = new Dictionary<string, string>();

            result.Add("lb", Regex.Match(line, @"lb=(.*?)$").Groups[1].Value); // label
            result.Add("ts", Regex.Match(line, @"ts=(\d+)").Groups[1].Value); // timestamp (epoch in ms)
            result.Add("t", Regex.Match(line, @"t=(\d+)").Groups[1].Value); // response time
            result.Add("s", Regex.Match(line, @"\ss=(\w+)").Groups[1].Value); // success
            result.Add("na", Regex.Match(line, @"na=([\+\-0-9]+)").Groups[1].Value); // concurrent users, can be negative!

            //Log.WriteLine("parse result lb=" + result["lb"]);

            return result; // new string[] { lb, ts, t, s, na};
        }

    }
}
