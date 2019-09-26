using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace rpg.common
{
    /// <summary>
    /// Utilities
    /// </summary>
    static public class Utils
    {

        /// <summary>
        /// Test if given string (or character) is a numeric value or not
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static bool IsNumeric(this string s)
        {
            float output;
            return float.TryParse(s, out output);
        }

        /// <summary>
        /// Find first occurence of pattern in lines
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="valuePattern"></param>
        /// <returns></returns>
        public static string ExtractValueByPatternFirst(string[] lines, string valuePattern)
        {
            string result = "";
            Regex regex = new Regex(valuePattern);
            foreach (string line in lines)
            {
                if (regex.IsMatch(line))
                {
                    result = regex.Match(line).Groups[1].Value;
                    return result;
                }
            }
            Log.WriteLine("WARNING did not find value pattern ["+valuePattern+"]");
            return result;
        }

        /// <summary>
        /// Find last occurence of pattern in lines
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="valuePattern"></param>
        /// <returns></returns>
        public static string ExtractValueByPatternLast(string[] lines, string valuePattern)
        {
            string result = "";
            Regex regex = new Regex(valuePattern);

            foreach (string line in lines.Reverse())
            {
                if (regex.IsMatch(line))
                {
                    result = regex.Match(line).Groups[1].Value;
                    return result;
                }
            }
            return result;
        }

        /// <summary>
        /// Convert Jmeter epoch (ms) to DateTime
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static DateTime ParseJMeterEpoch(string s)
        {
            // convert epoch to datetime
            long ts_epoch = long.Parse(s);
            DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return dt.AddMilliseconds(ts_epoch).ToLocalTime(); // mwn12032018
        }

        /// <summary>
        /// Convert malformed decimal string to workable decimal string (acc to system locale)
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string NormalizeFloatString(string value)
        {
            string outValue;
            char systemDecimalSeparator = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0];
            // maakt niet uit wat er binnen komt (. of , als decimal separator): vervangen door system decimalseparator
            outValue = value.Replace(',', systemDecimalSeparator);
            outValue = outValue.Replace('.', systemDecimalSeparator);
            return outValue;
        }

        /// <summary>
        /// Convert Jmeter time to intermediate seconds
        /// </summary>
        /// <returns></returns>
        public static string jmeterTimeToSeconds(string jmValueStr)
        {
            //values: 01003_linkhelp,87,58,20,29,91,385,14,2445,0.00,0.1,17.8,262.78
            float f;
            string strVal;
            try
            {
                strVal = NormalizeFloatString(jmValueStr);
                if (float.TryParse(strVal, out f))
                    f = f / 1000;
                else
                    throw new Exception("failover to catch");
            }
            catch
            {
                Log.WriteLine(string.Format("WARNING cannot parse value [{0}]", jmValueStr));
                return jmValueStr;
            }
            return f.ToString("0.000");
        }

        /// <summary>
        /// Number format to report measure format (. as decimal)
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static string ToMeasureFormat(string val)
        {
            return val.Replace(",", ".");
        }

        /// <summary>
        /// Float string to intermediate float string
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string NormalizeFloat(string value)
        {
            return NormalizeFloatString(value);
        }

        /// <summary>
        /// Check if it is XML format and contains key
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="searchString"></param>
        public static bool IsXMLWithKey(string filename, string searchString)
        {
            StreamReader sr = new StreamReader(filename);
            string firstline = sr.ReadLine();
            // is it an xml file?
            if (!firstline.Contains("<?xml"))
                return false;
            // does it contain searchstring
            while (!sr.EndOfStream)
            {
                if (sr.ReadLine().Contains(searchString))
                    return true;
            }
            // if not: return false
            return false;
        }

        /// <summary>
        /// Is this a CSV with keyword?
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="searchString"></param>
        /// <returns></returns>
        public static bool IsCSVWithKey(string fileName, string searchString)
        {
            StreamReader sr = new StreamReader(fileName);
            // does it contain searchstring
            while (!sr.EndOfStream)
            {
                if (sr.ReadLine().Contains(searchString))
                    return true;
            }
            // if not: return false
            return false;

        }

        /// <summary>
        /// Correct transaction name so guarantee a problem-free merging process
        /// </summary>
        /// <param name="suggestedName"></param>
        /// <returns></returns>
        public static string NormalizeTransactionName(string suggestedName)
        {
            //string name = suggestedName;
            //name = name.Replace('/','|');   // dangerous for sed statement in templategenerator (for now)
            //name = name.Replace('\\','|');  // dangerous for sed statement in templategenerator (for now)
            //name = name.Replace(":", "-");
            //name = name.Replace("=", "-");
            //name = name.Replace(".", "");   // dangerous for sed statement in templategenerator (for now)
            //name = name.Replace("<", "");
            //name = name.Replace(">", "");
            //name = name.Replace("\"", "");
            //name = name.Replace("\'", "");
            //name = name.Replace("[", "(");
            //name = name.Replace("]", ")");
            //name = name.Replace('{', '|'); // sed in template generator is sensitive to curly brackets (replaces initial fix in post
            //name = name.Replace('}', '|');
            //name = name.Trim();             // just more beautiful
            //name = name.Replace("  "," ");  // template generator is failing on this

            // trial for testpurposes, not effective yet, appears only in log for now
            string newName = "";
            foreach (char c in suggestedName)
            {
                // characters allowed: a-z A-Z 0-9 - _ rest is replaced by _
                if (Regex.IsMatch(c.ToString(), $"[a-zA-Z0-9-]"))
                    newName = string.Concat(newName, c);
                else
                    newName = string.Concat(newName, '_');
            }
            Log.WriteLine(string.Format("normalize transactionname org=[{0}] new=[{1}]", suggestedName, newName));

            return newName;
        }

    }
}
