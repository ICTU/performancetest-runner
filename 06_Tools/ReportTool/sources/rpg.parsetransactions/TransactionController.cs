using rpg.common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace rpg.parsetransactions
{
    abstract class TransactionController
    {
        public TransactionDetails _transactionDetails = new TransactionDetails();
        public Intermediate _variables = new Intermediate();

        public string[] _transactionNames;

        public const string STANDARDTRSTOTALKEY = "#Overall Response Time#";
        public const string AGGREGATEDTRSNAME = "AGGREGATED";
        // transaction name pattern for aggregation
        public const string REPORTTRANSACTIONNAMEPATTERN = @"\d\d_"; // TODO naar app config
        public const string TRANSACTIONNAMESFILENAME = "Transactions.csv";

        public const string AVGOF90PERCENTILES = "avgof90percentiles";
        public const string AVGOFMEDIAN = "avgofmedians";
        public const string AVGOF95PERCENTILES = "avgof95percentiles";
        public const string AVGOFAVERAGES = "avgofaverages";
        public const string TRANSACTIONSTOTAL = "transactionstotal";
        public const string TRANSACTIONSFAILED = "transactionsfailed";

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

            // check if parsed data is usable for further processing
            CheckDataViable(parameters);

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
            TransactionValueAggregate transactionAggregate = new TransactionValueAggregate();

            // foreach transactionlines
            foreach (string transactionName in _transactionNames)
            {
                // only include in agggregation if transactionname matches 'report transacton name pattern'
                if (IsSummarizeTransaction(transactionName) && _transactionDetails.items.ContainsKey(transactionName))
                {
                    TransactionValue trs = new TransactionValue(_transactionDetails.items[transactionName]);
                    transactionAggregate.AddTransaction(trs);
                    cnt++;
                }
            }

            // give up if no summarizable transactions found (try to prevent weird crashes lateron)
            if (0 == cnt)
            {
                string msg = "no transaction names found that meet naming convention (" + REPORTTRANSACTIONNAMEPATTERN + ")";
                Log.WriteLine("SEVERE: " + msg);
                throw new Exception(msg);
            }

            transactionAggregate.Aggregate();

            // write aggregated transaction line
            _transactionDetails.Add(AGGREGATEDTRSNAME, transactionAggregate.ToString());

            // isolate most important values as variable for history graph -> measure format (later to variable category?)
            _variables.Add(AVGOF90PERCENTILES, Utils.ToMeasureFormat(transactionAggregate.p90));
            _variables.Add(AVGOFMEDIAN, Utils.ToMeasureFormat(transactionAggregate.median));
            _variables.Add(AVGOF95PERCENTILES, Utils.ToMeasureFormat(transactionAggregate.p95));
            _variables.Add(AVGOFAVERAGES, Utils.ToMeasureFormat(transactionAggregate.avg));
            _variables.Add(TRANSACTIONSTOTAL, transactionAggregate.cnt);
            _variables.Add(TRANSACTIONSFAILED, transactionAggregate.fail);

            Log.WriteLine(string.Format("{0} of {1} transactions aggregated", cnt, _transactionNames.Length));
        }

        /// <summary>
        /// Check if pased data is enough to work with, generic check
        /// checks are now implemented in technology parts (aggregate part), but has to move to generic level here (TODO)
        /// </summary>
        /// <param name=""></param>
        public void CheckDataViable(ParamInterpreter parameters)
        {
            //
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
                    string trsName = Utils.NormalizeTransactionName(trsRegex.Match(line).Groups[1].Value);
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

            // write transaction data
            string fileName = parameters.Value("intermediatefile");
            Log.WriteLine("transaction data: "+fileName );
            _transactionDetails.WriteToFile(fileName);

            // Extra: file with only headers
            _transactionDetails.WriteToFileFieldDefinitions(fileName, "fieldnames", string.Join(TransactionValue.LISTSEPARATOR.ToString(), TransactionValue.fieldnames));

            // Extra: file with all transactionnames
            _transactionDetails.WriteToFileTranactionnames(fileName);

            // write transaction variables
            string varFileName = parameters.Value("intermediatefilevars");
            Log.WriteLine("transaction variable data: " + varFileName);
            _variables.WriteToFile(varFileName);
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
