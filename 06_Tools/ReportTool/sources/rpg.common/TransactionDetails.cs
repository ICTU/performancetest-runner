using System;
using System.Collections.Generic;
using System.IO;

namespace rpg.common
{
    /// <summary>
    /// Class representing transaction (value series) details
    /// </summary>
    public class TransactionDetails
    {
        /// <summary> intermediate item list </summary>
        public Intermediate items = new Intermediate();

        /// <summary> keyvaluepair separator </summary>
        public const char KEYVALUESEPARATOR = '=';
        
        /// <summary>
        /// Add transaction details key-value pair or replace value of existing key (warning)
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void Add(string name, string value)
        {
            if (items.ContainsKey(name))
            {
                Log.WriteLine("WARNING: replacing value of existing key ["+name+"]");
                items[name] = value;
            }
            else
                items.Add(name, value);
        }

        /// <summary>
        /// Add transactionDetails to this
        /// </summary>
        /// <param name="transactionDetails"></param>
        public void Add(TransactionDetails transactionDetails)
        {
            this.items.Add(transactionDetails.items);
        }

        /// <summary>
        /// Write content to file
        /// </summary>
        /// <param name="fileName"></param>
        public void WriteToFile(string fileName)
        {
            using (StreamWriter sw = new StreamWriter(fileName))
            {
                foreach (KeyValuePair<string, string> item in items)
                {
                    sw.WriteLine(item.Key + KEYVALUESEPARATOR + item.Value);
                }
            }
        }

        /// <summary>
        /// Write definitions to definitions file (header)
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void WriteToFileFieldDefinitions(string fileName, string key, string value)
        {
            Log.WriteLine("write .definitions file...");
            File.WriteAllText(fileName+".definitions", key + KEYVALUESEPARATOR + value);
        }

        /// <summary>
        /// Write all transactionnames to csv file, compatible with reporttemplategenerator Transactions.csv
        /// </summary>
        /// <param name="v"></param>
        public void WriteToFileTranactionnames(string fileName)
        {
            Log.WriteLine("write .transactionnames file...");

            string targetFileName = fileName + ".transactionnames";
            using (StreamWriter sw = new StreamWriter(targetFileName))
            {
                foreach (KeyValuePair<string, string> item in items)
                {
                    sw.WriteLine(item.Key);
                }
            }
        }

    }
}
