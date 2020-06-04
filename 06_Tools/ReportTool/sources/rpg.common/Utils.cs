using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace rpg.common
{
    static public class Category
    {
        public static string Variable = "var";
        public static string Transaction = "trs";
    }

    static public class Entity
    {
        public static string None = "-";
        public static string Runinfo = "runinfo";
        public static string Generic = "generic";
        public static string Transaction = "trs";
    }

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
                //Log.WriteLine("DEBUG handing line: "+line);
                if (regex.IsMatch(line))
                {
                    //Log.WriteLine("DEBUG match! applying pattern: " + valuePattern + "/" + line);
                    result = regex.Match(line).Groups[1].Value;
                    //Log.WriteLine("DEBUG match: "+result);
                    return result;
                }
            }
            return result;
        }

        /// <summary>
        /// Find lowest occurence of pattern in lines
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="valuePattern"></param>
        /// <returns>lowest value in the list</returns>
        public static string ExtractValueByPatternLowest(string[] lines, string valuePattern)
        {
            string result;
            Regex regex = new Regex(valuePattern);

            long newDT;
            long lowestDT = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            foreach (string line in lines)
            {
                if (regex.IsMatch(line))
                {
                    result = regex.Match(line).Groups[1].Value;
                    newDT = Convert.ToInt64(result);
                    //Log.WriteLine("newDT: " + newDT);
                    if (lowestDT > newDT)
                    {
                        lowestDT = newDT;
                    }
                }
            }

            result = lowestDT.ToString();
            return result;
        }

        /// <summary>
        /// Find highest occurence of pattern in lines
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="valuePattern"></param>
        /// <returns>highest value in the list</returns>
        public static string ExtractValueByPatternHighest(string[] lines, string valuePattern)
        {
            string result;
            Regex regex = new Regex(valuePattern);

            long newDT;
            long highestDT = new long();

            foreach (string line in lines)
            {
                if (regex.IsMatch(line))
                {
                    result = regex.Match(line).Groups[1].Value;
                    newDT = Convert.ToInt64(result);
                    if (highestDT < newDT)
                    {
                        highestDT = newDT;
                    }
                }
            }

            result = highestDT.ToString();
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
        /// Convert any decimal string to decimal string according to System Locale
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ToSystemFloatString(string value)
        {
            return ToAnyFloatString(value, System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0]);
        }

        /// <summary>
        /// Convert any decimal string to intermediate general float string
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ToIntermediateFloatString(string value)
        {
            return ToAnyFloatString(value, Intermediate.DECIMALSEPARATORINTERMEDIATE);
        }

        /// <summary>
        /// Override for double
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ToIntermediateFloatString(double value)
        {
            return ToIntermediateFloatString(value.ToString());
        }


        /// <summary>
        /// convert any decimal string to intermediate measure float string
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ToMeasureFloatString(string value)
        {
            return ToAnyFloatString(value, Intermediate.DECIMALSEPARATORMEASURES);
        }

        public static string ToAnyFloatString(string value, char separator)
        {
            string outValue;
            // maakt niet uit wat er binnen komt (. of , als decimal separator): vervangen door decimalseparator
            outValue = value.Replace(',', separator);
            outValue = outValue.Replace('.', separator);
            return outValue;
        }

        /// <summary>
        /// Convert Jmeter time to intermediate seconds
        /// </summary>
        /// <returns></returns>
        public static string jmeterTimeToIntermediateSecondsString(string jmValueStr)
        {
            //values: 01003_linkhelp,87,58,20,29,91,385,14,2445,0.00,0.1,17.8,262.78
            float f;
            string strVal;
            try
            {
                //strVal = ToIntermediateFloatString(jmValueStr);
                strVal = ToSystemFloatString(jmValueStr);

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

            //return f.ToString("0.000");

            // elk formaat naar Intermediate float format
            return ToIntermediateFloatString(f.ToString());
        }

        /// <summary>
        /// Number format to report measure format (. as decimal)
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static string ToMeasureFormatString(string value)
        {
            return ToMeasureFloatString(value);
        }

        /// <summary>
        /// Float string to intermediate float string
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string NormalizeFloatString(string value)
        {
            return ToIntermediateFloatString(value);
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
            string newName = "";

            foreach (char c in suggestedName.Trim())
            {
                // characters allowed: a-z A-Z 0-9 - _ rest is replaced by _
                if (Regex.IsMatch(c.ToString(), $"[a-zA-Z0-9-]"))
                    newName = string.Concat(newName, c);
                else
                    newName = string.Concat(newName, '_');
            }

            // only log message if transaction name is changed
            if (string.Compare(suggestedName, newName) > 0 )
                Log.WriteLine(string.Format("transaction name normalized org=[{0}] new=[{1}]", suggestedName, newName));

            return newName;
        }

        /// <summary>
        /// Discover and return the decimal separator character in value
        /// </summary>
        /// <param name="returnValue"></param>
        /// <returns></returns>
        public static char GetDecimalChar(string value)
        {
            // return first non-numeric character
            return Regex.Match(value, "[.,]").Value[0];
        }
    }
}
