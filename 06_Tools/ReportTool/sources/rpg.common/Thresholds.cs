using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace rpg.common
{
    internal class Threshold
    {
        public string eval;
        public double th1;
        public double th2;
    }

    /// <summary>
    /// Handling threshold data
    /// </summary>
    public class Thresholds
    {
        List<Threshold> thresholdCollection = new List<Threshold>();

        double genericTh1 = 1;
        double genericTh2 = 1;
        string genericTag = "generic";

        /// <summary> decimal separator character </summary>
        public static char decimalSeparator = ','; // moet uit de currentculture te halen zijn, werkt zo snel niet
        /// <summary> thousand separator character </summary>
        public static char thousandSeparator = '.';

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="project"></param>
        public Thresholds(string project)
        {
            Load(project);
        }

        /// <summary>
        ///  Load threshold values for this project
        /// </summary>
        /// <param name="project"></param>
        public void Load(string project)
        {
            DataAccess da = new DataAccess(project);
            bool genericFound = false;

            List<object> result = da.GetThresholds();
            foreach (List<string> row in result)
            {
                Threshold th = new Threshold();
                th.eval = row[0];
                th.th1 = StringValueToDouble(row[1]);
                th.th2 = StringValueToDouble(row[2]);

                // generic defaults
                if (th.eval == genericTag)
                {
                    genericTh1 = th.th1;
                    genericTh2 = th.th2;
                    genericFound = true;
                }
                else thresholdCollection.Add(th);
            }
            if (!genericFound)
                Log.WriteLine("Warning: 'generic' threshold not found for project "+project+"; fallback to default "+genericTh1.ToString()+" seconds");

            da.DeInitialize();
        }

        /// <summary>
        /// Determine threshold (color) key
        /// </summary>
        /// <param name="orgKey"></param>
        /// <returns></returns>
        public static string GetThresholdColorKey(string orgKey)
        {
            return orgKey+"_c";
        }

        /// <summary>
        /// Determine threshold reference key (value containing 2 values: th1, th2)
        /// </summary>
        /// <param name="orgKey"></param>
        /// <returns></returns>
        public static string GetThresholdReferenceKey(string orgKey)
        {
            return orgKey + "_th";
        }

        /// <summary>
        /// Generate
        /// </summary>
        /// <param name="intermediate"></param>
        /// <param name="green"></param>
        /// <param name="yellow"></param>
        /// <param name="red"></param>
        /// <returns></returns>
        public Intermediate GenerateThresholdValuesForTransactions(Intermediate intermediate, string green, string yellow, string red)
        {
            Intermediate thIntermediate = new Intermediate();

            foreach(KeyValuePair<string, string> pair in intermediate)
            {
                // add threshold color code for all value fields of this transaction
                thIntermediate.Add( GetThresholdColorKey(pair.Key), GenerateThresholdColorValuesForTransaction(pair.Key, pair.Value, green, yellow, red));
                // add threshold reference values (th[0,1]) for this transaction
                thIntermediate.Add( GetThresholdReferenceKey(pair.Key), GenerateThresholdReferenceForTransaction(pair.Key, pair.Value));
            }
            return thIntermediate;
        }

        /// <summary>
        /// Add intermediate threshold reference values
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private string GenerateThresholdReferenceForTransaction(string key, string value)
        {
            List<string> outValues = new List<string>();

            // what are the threshold values for this key
            Threshold th = GetThresholdForKey(key);

            // add the thresholds as values in a new threshold key
            outValues.Add(Utils.ToIntermediateFloatString(th.th1));
            outValues.Add(Utils.ToIntermediateFloatString(th.th2));

            return string.Join(Intermediate.LISTSEPARATOR.ToString(), outValues.ToArray());
        }


        /// <summary>
        /// Add intermediate colorcode values according to threshold
        /// </summary>
        /// <param name="key"></param>
        /// <param name="valueList"></param>
        /// <param name="green"></param>
        /// <param name="yellow"></param>
        /// <param name="red"></param>
        /// <returns></returns>
        private string GenerateThresholdColorValuesForTransaction(string key, string valueList, string green, string yellow, string red)
        {
            List<string> outValues = new List<string>();
            string colorCode;
            double value;

            // what are the threshold values for this key
            Threshold th = GetThresholdForKey(key);

            foreach(string val in valueList.Split(Intermediate.LISTSEPARATOR))
            {
                value = StringValueToDouble(val);

                // not a number (evaluatie value==double.NaN werkt niet!)
                if (double.IsNaN(value)) colorCode = "";
                else if (value < th.th1) colorCode = green;
                else if (value >= th.th2) colorCode = red;
                else colorCode = yellow;

                outValues.Add(colorCode);
            }
            return string.Join( Intermediate.LISTSEPARATOR.ToString(), outValues.ToArray() );
        }

        /// <summary>
        /// Find LAST threshold that matches the key
        ///   so general patterns should be first in list, specific patterns at the end
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private Threshold GetThresholdForKey(string key)
        {
            Threshold result = new Threshold();

            // if none found, set generic
            result.eval = genericTag;
            result.th1 = genericTh1;
            result.th2 = genericTh2;

            foreach (Threshold threshold in thresholdCollection)
            {
                Regex re = new Regex(threshold.eval);
                if (re.IsMatch(key))
                {
                    // find last matching threshold
                    result = threshold;
                }
            }

            Log.WriteLine(String.Format("threshold match for {0}=[{1}]", key, result.eval));

            return result;
        }

        /// <summary>
        /// Convert string number any format to double
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static double StringValueToDouble(string value)
        {
            try
            {
                return double.Parse(Utils.ToSystemFloatString(value));

                //// normale decimal separator
                //if (value.Contains( decimalSeparator ))
                //{
                //    return double.Parse(value);
                //}

                //// thousend separator als decimal separator
                //if (value.Contains( thousandSeparator ))
                //{
                //     return double.Parse( value.Replace( thousandSeparator, decimalSeparator) );
                //}

                //// of integer getal, of lege string
                //return double.Parse(value);
            }
            catch (Exception)
            {
                // als value == lege string
            }

            // no separator
            return double.NaN;
        }

        ///// <summary>
        ///// Convert double to a string value with .000 format
        ///// </summary>
        ///// <param name="value"></param>
        ///// <returns></returns>
        //public static string DoubleValueToSTring(double value)
        //{
        //    string formatString = string.Format("0{0}000", decimalSeparator);
        //    return value.ToString(formatString);
        //}
    }
}
