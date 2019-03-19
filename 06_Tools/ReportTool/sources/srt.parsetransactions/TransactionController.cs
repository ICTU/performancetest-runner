using srt.common;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace srt.parsetransactions
{
    abstract class TransactionController
    {
        public TransactionDetails _transactionDetails = new TransactionDetails();
        public string[] _transactionNames;

        public const string STANDARDTRSTOTALKEY = "#Overall Response Time#";
        public const string AGGREGATEDTRSNAME = "AGGREGATED";
        // transaction name pattern for aggregation
        public const string REPORTTRANSACTIONNAMEPATTERN = @"\d\d_"; // TODO naar app config

        public virtual void Parse(ParamInterpreter parameters)
        {
            // check input file format (is it what we expect?)
            try
            {
                CheckInputfileFormat(parameters);
            }
            catch (Exception e)
            {
                throw new FormatException("Cannot parse, input file format problem ["+e.Message+"]");
            }

            // perform loadgen specific parse (in derived classes)
            DoParse(parameters);

            // generate aggregated data (faultperc)
            GenerateAggregates(parameters);

            // convert data to confom system locale settings (output formatting is done during merge)
            FormatData();

            // Write intermediate file
            WriteIntermediate(parameters);
        }

        // Implemented by derived classes (standard returns false to force override)
        public abstract void CheckInputfileFormat(ParamInterpreter parameters);

        // Implementerd by derived classes
        public abstract void DoParse(ParamInterpreter parameters);

        /// <summary>
        /// Generate aggregate data (total faults, average of percentiles...)
        /// </summary>
        /// <param name="parameters"></param>
        public void GenerateAggregates(ParamInterpreter parameters)
        {
            int cnt = 0;
            Log.WriteLine("generate aggregated data...");
            // scrape and aggregate data from transaction data
            TransactionValueAggregate aggregateValue = new TransactionValueAggregate();

            // foreach transacitonlines
            foreach (string transactionName in _transactionNames)
            {
                // only include in agggregation if transactionname matches 'report transacton name pattern'
                if (IsSummarizeTransaction(transactionName) && _transactionDetails.items.ContainsKey(transactionName))
                {
                    TransactionValue trs = new TransactionValue(_transactionDetails.items[transactionName]);
                    aggregateValue.Evaluate(trs);
                    cnt++;
                }
            }
            aggregateValue.Aggregate();
            _transactionDetails.Add(AGGREGATEDTRSNAME, aggregateValue.ToString());

            Log.WriteLine(string.Format("{0} of {1} transactions aggregated", cnt, _transactionNames.Length));
        }

        /// <summary>
        /// Does this transactionname match the pattern for a 'report transaction'
        /// </summary>
        /// <param name="transactionName"></param>
        /// <returns></returns>
        private bool IsSummarizeTransaction(string transactionName)
        {
            return Regex.IsMatch(transactionName, REPORTTRANSACTIONNAMEPATTERN);
        }

        /// <summary>
        /// Extraheren transactienamen, niet direct gebruiken!
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public string[] ExtractTransactionNames(string[] lines, string pattern)
        {
            List<string> transactionNames = new List<string>();
            Regex trsRegex = new Regex(pattern);
            int cnt = 0;
            foreach (string line in lines)
            {
                if (trsRegex.IsMatch(line)) // let op: moet ook goed gaan als trs alleen fout gegaan is, dus niet alleen trsBusyOk
                {
                    string trsName = trsRegex.Match(line).Groups[1].Value;
                    if (!transactionNames.Contains(trsName)) // we willen zowel project- als standaard transacties meenemen (incl #overall en TInit)
                    {
                        //Log.WriteLine("tranaction name identified: " + trsName);
                        transactionNames.Add(trsName);
                        cnt++;
                    }
                }
            }
            Log.WriteLine(cnt.ToString()+" transaction names identified");
            return transactionNames.ToArray();
        }


        /// <summary>
        /// Write intermediate format to file
        /// </summary>
        /// <param name="parameters"></param>
        public void WriteIntermediate(ParamInterpreter parameters)
        {
            Log.WriteLine("Writing intermediate file...");

            string fileName = parameters.Value("intermediatefile");
            Log.WriteLine( fileName );

            _transactionDetails.WriteToFile(fileName);

            // Extra: file with only headers
            _transactionDetails.WriteToFileDef(fileName, "fieldnames", string.Join(TransactionValue.LISTSEPARATOR.ToString(), TransactionValue.fieldnames));
        }

        /// <summary>
        /// Format the data to current culture format (make it easy to calculate)
        /// </summary>
        public void FormatData()
        {
            Log.WriteLine("Formatting data (decimal separator)...");
            foreach (string trsName in _transactionNames)
            {
                _transactionDetails.items[trsName] = Utils.NormalizeFloatString(_transactionDetails.items[trsName]);
            }
        }

        /// <summary>
        /// Duplicate variable to other name to match standard (Silk) format
        /// </summary>
        /// <param name="orgName"></param>
        /// <param name="duplicateName"></param>
        public void DuplicateTransactionData(string orgName, string duplicateName)
        {
            _transactionDetails.items.Duplicate(orgName, duplicateName);
            AddTransactionName(duplicateName);
        }

        /// <summary>
        /// Add string to string array
        /// </summary>
        /// <param name="newName"></param>
        /// <returns></returns>
        public void AddTransactionName(string newName)
        {
            List<string> lst = new List<string>();
            foreach (string name in _transactionNames)
                lst.Add(name);
            lst.Add(newName);
            _transactionNames = lst.ToArray();
        }

    }
}
