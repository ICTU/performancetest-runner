using System;
using System.Collections.Generic;
using System.Linq;


namespace rpg.common
{
    /// <summary>
    /// Intermediate specialization for measure data
    /// </summary>
    public class MeasureDetails
    {
        /// <summary> decimal separator for measure data </summary>
        public const char MEASUREDECIMALSEPARATOR = '.';
        /// <summary> field separator for measure data </summary>
        public const char MEASURETFIELDSEPARATOR = ',';
        /// <summary> values (intermediate key=value pairs) </summary>
        public Intermediate items = new Intermediate();

        /// <summary>
        /// xx.x -> xx,x 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private string ConformValue(string value)
        {
            return value.Replace('.', ',');
        }

        /// <summary>
        /// = -> _
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private string ConformName(string name)
        {
            return name.Replace('=', '_');
        }

        /// <summary>
        /// Add new key=value or (if key exist): add value to list of values
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="list"></param>
        private void AddToList(string key, string value, Intermediate list)
        {
            if (list.ContainsKey(key))
                list[key] += Intermediate.LISTSEPARATOR + value;
            else
                list.Add(key, value);
        }

        /// <summary>
        /// Add new key=value or (if key exist): add value to list of values
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void Add(string name, string value)
        {
            AddToList(name, value, items);
        }

        /// <summary>
        /// Format read measure data and replace SILK proprietary separators with parameter decimal- and listseparator
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="srcDecSeparator"></param>
        /// <returns></returns>
        public string[] FormatMeasureData(string[] keys, char srcDecSeparator)
        {
            // convert values
            Log.Write("format measure values [");

            foreach (string key in keys)
            {
                items[key] = items[key].Replace(srcDecSeparator, MEASUREDECIMALSEPARATOR);
                items[key] = items[key].Replace(Intermediate.LISTSEPARATOR, MEASURETFIELDSEPARATOR);
                Log.Write(".");
            }
            Log.WriteLine("]");

            // convert names
            Log.Write("correct measure names if necessary [");

            Intermediate newItems = new Intermediate();
            List<string> newKeys = new List<string>();
            foreach (KeyValuePair<string, string> item in items)
            {
                newKeys.Add( ConformName(item.Key) );
                newItems.Add(newKeys.Last(), item.Value);
                Log.Write(".");
            }
            Log.WriteLine("]");
            items = newItems;
            return newKeys.ToArray();
        }

        /// <summary>
        /// Format data part of series including decimal and list separators
        /// </summary>
        /// <param name="key"></param>
        /// <param name="searchTag"></param>
        /// <param name="targetTag"></param>
        public void FormatDataReplace(string key, char searchTag, char targetTag)
        {
            items[key] = items[key].Replace(searchTag, targetTag);
        }

        /// <summary>
        /// Format data part of series including decimal and list separators
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="searchTag"></param>
        /// <param name="targetTag"></param>
        public void FormatDataReplace(string[] keys, char searchTag, char targetTag)
        {
            foreach (string key in keys)
            {
                FormatDataReplace(key, searchTag, targetTag);
            }
        }

        /// <summary>
        /// Get current system decimal separator
        /// </summary>
        /// <returns></returns>
        public static char GetSystemDecimalSeparator()
        {
            return System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator[0];
        }

        /// <summary>
        /// Get current system thousandseparator
        /// </summary>
        /// <returns></returns>
        public static char GetSystemThousandSeparator()
        {
            return System.Globalization.NumberFormatInfo.CurrentInfo.NumberGroupSeparator[0];
        }


        /// <summary>
        /// Trim first x values of series
        /// </summary>
        /// <param name="numOfEntries"></param>
        public void TrimLeftValues(int numOfEntries)
        {
            // als aantal metingen < numOfEntries, dan numOfEntries = metingen
            int cnt = (numOfEntries > items.Count) ? items.Count : numOfEntries;

            if (cnt != 0)
            {
                string[] keys = items.GetKeys();
                foreach (string key in keys)
                {
                    // values uit elkaar trekken naar items in een stringlist
                    List<string> values = items[key].Split(Intermediate.LISTSEPARATOR).ToList();

                    // eerste x entries verwijderen
                    for (int i = 0; i < cnt; i++)
                        values.RemoveAt(0);

                    // list terug naar string
                    items[key] = string.Join(Intermediate.LISTSEPARATOR.ToString(), values.ToArray());
                }
            }
        }

        /// <summary>
        /// Trim first x values of series
        /// </summary>
        /// <param name="numOfEntries"></param>
        public void TrimRightValues(int numOfEntries)
        {
            // als aantal metingen < numOfEntries, dan numOfEntries = metingen
            int cnt = (numOfEntries > items.Count) ? items.Count : numOfEntries;

            if (cnt != 0)
            {
                string[] keys = items.GetKeys();
                foreach (string key in keys)
                {
                    // values uit elkaar trekken naar items in een stringlist
                    List<string> values = items[key].Split(Intermediate.LISTSEPARATOR).ToList();

                    // laatste x entries verwijderen
                    for (int i = 0; i < numOfEntries; i++)
                        values.RemoveAt(values.Count - 1);

                    // list terug naar string
                    items[key] = string.Join(Intermediate.LISTSEPARATOR.ToString(), values.ToArray());
                }
            }
        }

        public double[] GetValuesAsDoubleArray(string key)
        {
            List<double> convert = new List<double>();
            string evaluated = "NOVALUE";
            char separator = GetSystemDecimalSeparator();

            try
            {
                foreach (string strVal in items[key].Split(MEASURETFIELDSEPARATOR))
                {
                    evaluated = strVal;
                    convert.Add(double.Parse(evaluated.Replace(MEASUREDECIMALSEPARATOR, separator)));
                }
            }
            catch (Exception e)
            {
                Log.WriteLine("WARNING conversion of measure data array to double type failed, key ["+ key +"] value ["+ evaluated +"]");
                throw e;
            }
            return convert.ToArray();
        }

    }
}
