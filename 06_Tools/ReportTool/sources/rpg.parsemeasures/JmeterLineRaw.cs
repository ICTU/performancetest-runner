using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace rpg.parsemeasures
{
    public class JmeterLineRaw
    {
        // how to recognise a main sample line
        // <httpSample t="28"; <sample t="786"; 
        public static string SamplePattern = "ample ";

        // regex patterns for extraction out of raw JTL files
        private static string regex_ts = "ts=\"(\\d+)\""; // ts=timestamp
        private static string regex_t = "t=\"(\\d+)\""; // t=response time
        private static string regex_na = "na=\"(\\d+)\""; // na=concurrency
        private static string regex_s = "\\ss=\"(\\w+)\""; // s=success (true/false)
        private static string regex_lb = "lb=\"(.*?)\""; // lb=label

        public static Dictionary<string, string> GetSampleAttributes(string line)
        {
            Dictionary<string, string> attributes = new Dictionary<string, string>();
            attributes.Add("ts", Regex.Match(line, regex_ts).Groups[1].Value);
            attributes.Add("t", Regex.Match(line, regex_t).Groups[1].Value);
            attributes.Add("na", Regex.Match(line, regex_na).Groups[1].Value);
            string s = Regex.Match(line, regex_s).Groups[1].Value;
            attributes.Add("s", ((s == "false") || (s == "0")) ? "false" : "true");
            attributes.Add("lb", Regex.Match(line, regex_lb).Groups[1].Value);

            return attributes;
        }
    }

}
