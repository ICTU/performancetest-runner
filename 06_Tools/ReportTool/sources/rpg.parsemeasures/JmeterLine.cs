using System.Text.RegularExpressions;

namespace rpg.parsemeasures
{
    class JmeterLine
    {
        /// <summary>
        /// substring position: lb=0; ts=1; t=2; s=3; na=4
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public static string[] Parse(string line)
        {
            string lb = Regex.Match(line, @"lb=(.+)$").Groups[1].Value; // label
            string ts = Regex.Match(line, @"ts=(\d+)").Groups[1].Value; // timestamp (epoch in ms)
            string t = Regex.Match(line, @"t=(\d+)").Groups[1].Value; // response time
            string s = Regex.Match(line, @"\ss=(\w+)").Groups[1].Value; // success
            string na = Regex.Match(line, @"na=([\+\-0-9]+)").Groups[1].Value; // concurrent users, can be negative!

            return new string[] { lb, ts, t, s, na};
        }

    }
}
