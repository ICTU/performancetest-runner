using System;
using System.Linq;
using rpg.common;
using System.IO;
using System.Text.RegularExpressions;

namespace rpg.parsetransactions
{
    class JmeterTransactionController: TransactionController
    {
        private const string TRSNAMEGROUPPATTERNCSV = @"^([^,]+)\,\d";
        private const string JMETERTRSTOTALKEY = "TOTAL";

        private const string ALLTRSCSV = "transactionfilecsv_all";
        private const string SUCCESSTRSCSV = "transactionfilecsv_success";

        /// <summary>
        /// Check is format of input files is as expected
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public override void CheckInputfileFormat(ParamInterpreter parameters)
        {
            Log.WriteLine("checking success trs csv...");
            string successFilename = parameters.Value(SUCCESSTRSCSV);

            if (!Utils.IsCSVWithKey(successFilename, "TOTAL"))
                throw new FormatException(successFilename);

            Log.WriteLine("checking all trs csv...");
            string allFilename = parameters.Value(ALLTRSCSV);

            if (!Utils.IsCSVWithKey(allFilename, "TOTAL"))
                throw new FormatException(allFilename);
        }

        /// <summary>
        /// Do actual parsing for Jmeter transactiondata
        /// </summary>
        /// <param name="parameters"></param>
        public override void DoParse(ParamInterpreter parameters)
        {
            Log.WriteLine("Execute JMeter parser...");

            // 2 input files are mandatory (success=summary with only successfull transactions all=all transactions including faulty
            parameters.VerifyFileExists(SUCCESSTRSCSV);
            parameters.VerifyFileExists(ALLTRSCSV);

            // Read only success (TrsBusyOk) transactions to _transactionDetails key-value pair list
            ReadTransactionDataFromCSV(parameters.Value(SUCCESSTRSCSV));

            // Add failed executions to success transaction data
            EnrichFailed(parameters.Value(ALLTRSCSV));
        }

        /// <summary>
        ///  Read and convert Jmeter transactiondata from CSV (converted from jtl)
        /// </summary>
        /// <param name="fileName"></param>
        public void ReadTransactionDataFromCSV(string fileName)
        {
            Log.WriteLine( string.Format("read 'success' transaction data from CSV [{0}]...", fileName) );
            string[] lines = File.ReadAllLines(fileName);

            Log.WriteLine("discover transaction names...");
            _transactionNames = ExtractTransactionNames(lines, TRSNAMEGROUPPATTERNCSV);

            Log.WriteLine("read transaction data...");
            _transactionDetails = ReadTransactionDetailsFromCSV(lines, _transactionNames);

            Log.WriteLine("duplicate totals data to conform standard keyname...");
            DuplicateTransactionData(JMETERTRSTOTALKEY, STANDARDTRSTOTALKEY);
        }

        /// <summary>
        /// Min, max, avg, cnt transactiondata
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="trsNames"></param>
        private TransactionDetails ReadTransactionDetailsFromCSV(string[] lines, string[] trsNames)
        {
            string trsName;
            TransactionDetails td = new TransactionDetails();

            foreach (string line in lines)
            {
                trsName = Utils.NormalizeTransactionName(line.Split(',')[0]);

                if (trsNames.Contains(trsName))
                {
                    TransactionValue value = ExtractTrsDetailsCSV(line);
                    td.Add(trsName, value.ToString());
                }
            }
            return td;
        }

        /// <summary>
        /// Extract the results and put it into transactionDetails: cnt, min, max, avg, stdev
        /// </summary>
        /// <param name="line"></param>
        private TransactionValue ExtractTrsDetailsCSV(string line)
        {
            //sampler_label,aggregate_report_count,average,aggregate_report_median,aggregate_report_90%_line,aggregate_report_95%_line,aggregate_report_99%_line,aggregate_report_min,aggregate_report_max,aggregate_report_error%,aggregate_report_rate,aggregate_report_bandwidth,aggregate_report_stddev
            //setUp: bsh.shared.zoekgob,1,1697,1697,1697,1697,1697,1697,1697,"0,00%",",6",",0","0,00"
            string cLine = CorrectJmeterCSVLine(line);
            string[] parts = cLine.Split(',');

            // wat nog mist is toevoegen van een lege regel als alles fout gegaan is
            TransactionValue value = new TransactionValue();
            string executed = parts[1];

            value.cnt = parts[1];
            string cntExecuted = parts[1];
            value.avg = Utils.jmeterTimeToIntermediateSecondsString(parts[2]);
            value.median = Utils.jmeterTimeToIntermediateSecondsString(parts[3]);
            value.p90 = Utils.jmeterTimeToIntermediateSecondsString(parts[4]);
            value.p95 = Utils.jmeterTimeToIntermediateSecondsString(parts[5]);
            //6=99p not present in csv but not really useable as an application perf metric
            value.min = Utils.jmeterTimeToIntermediateSecondsString(parts[7]);
            value.max = Utils.jmeterTimeToIntermediateSecondsString(parts[8]);

            value.fail = NormalizeFail(Utils.NormalizeFloatString(parts[9].TrimEnd('%')), cntExecuted);
            value.stdev = Utils.jmeterTimeToIntermediateSecondsString(parts[12]);

            return value;
        }

        /// <summary>
        /// Add num of failed executions to base (good) transactiondata
        /// TODO and total num of executed
        /// </summary>
        /// <param name="allFileName"></param>
        private void EnrichFailed(string allFileName)
        {
            Log.WriteLine(string.Format("read 'all' transaction data from CSV [{0}]...", allFileName));
            string[] lines = File.ReadAllLines(allFileName);

            Log.WriteLine("discover transaction names...");
            // replace transactionnames, allLines contains more (incl 100% faulty) transactions than success
            _transactionNames = ExtractTransactionNames(lines, TRSNAMEGROUPPATTERNCSV);

            Log.WriteLine("read transaction data...");
            TransactionDetails allTransactionDetails = ReadTransactionDetailsFromCSV(lines, _transactionNames);

            // merge error data from 'all' transactions into already parsed 'success' transactiondata
            Log.WriteLine("merge fault data...");
            MergeFaultData(_transactionDetails, allTransactionDetails);
        }

        /// <summary>
        /// Merge fault data from 'all' transaction data into 'success' transaction data
        /// </summary>
        /// <param name="successTransactionDetails"></param>
        /// <param name="allTransactionDetails"></param>
        private void MergeFaultData(TransactionDetails successTransactionDetails, TransactionDetails allTransactionDetails)
        {
            // iterate over list of all transactions (allTransactionDetails)
            foreach (string trsName in allTransactionDetails.items.GetKeys())
            {
                // if found in successTransactionDetails: some exceptions, so: add only num of faults to existing success story
                if (successTransactionDetails.items.ContainsKey(trsName))
                {
                    // create working object for source- and target data
                    TransactionValue allValue = new TransactionValue(allTransactionDetails.items[trsName]);
                    TransactionValue successValue = new TransactionValue(successTransactionDetails.items[trsName]);
                    // if all fail (success=0): clear stats
                    if (successValue.cnt == "0") successValue = new TransactionValue();

                    // if faults have occured -> calculate fault field
                    if (!successValue.cnt.Equals(allValue.cnt, StringComparison.Ordinal))
                    {
                        successValue.fail = SubtractStrInt(allValue.cnt, successValue.cnt);
                        _transactionDetails.items[trsName] = successValue.ToString(); // replace num of failed
                        Log.WriteLine(string.Format("fault data: {0} faults added to {1}", successValue.fail, trsName));
                    }
                }
                // if not found in successTransactionDetails: 100% exception, so: add empty row with only number of faults
                else
                {
                    TransactionValue newSuccessValue = new TransactionValue();
                    TransactionValue allValue = new TransactionValue(allTransactionDetails.items[trsName]);
                    // only fill failed field from allvalue (=failed)
                    newSuccessValue.fail = allValue.cnt;
                    _transactionDetails.Add(trsName, newSuccessValue.ToString());
                    Log.WriteLine(string.Format("fault data: no-stats result ({0} faults) for {1}", newSuccessValue.fail, trsName));
                }
            }
        }

        /// <summary>
        /// Result is str1 - str2 as string
        /// </summary>
        /// <param name="str1"></param>
        /// <param name="str2"></param>
        /// <returns></returns>
        private string SubtractStrInt(string str1, string str2)
        {
            Int32 i = Int32.Parse(str1) - Int32.Parse(str2);
            return i.ToString();
        }

        /// <summary>
        /// Calculate success count from total executed - failed
        ///   has to be calculcated for Jmeter results as total is not total success
        /// </summary>
        /// <param name="executedStr"></param>
        /// <param name="failedStr"></param>
        /// <returns></returns>
        private string NormalizeSuccess(string executedStr, string failedStr)
        {
            string result = executedStr;
            try
            {
                // num of success = num executed - failed
                result = (int.Parse(executedStr) - int.Parse(failedStr)).ToString();
            }
            catch
            {
                // if no worky: return original executed (this could lead to faulty numbers)
                Log.WriteLine(string.Format("WARNING calculation of num of successful executions failed (total={0} failed={1})", executedStr, failedStr));
                result = executedStr;
            }
            //Log.WriteLine(string.Format("normalizing success count: executed={0}, failed={1} -> success={2}", executedStr, failedStr, result));
            return result;
        }

        /// <summary>
        /// Convert percentage to count
        /// </summary>
        /// <param name="failPercStr"></param>
        /// <param name="cntStr"></param>
        /// <returns></returns>
        private string NormalizeFail(string failPercStr, string cntStr)
        {
            float failPerc;
            float.TryParse(failPercStr, out failPerc);

            if (failPerc == 0) return "0";

            int cnt;
            int.TryParse(cntStr, out cnt);

            if (cnt == 0) return "0";

            try
            {
                return Math.Round(cnt * failPerc / 100).ToString();
            }
            catch
            {
                Log.WriteLine(string.Format("WARNING calculation of failed transactions failed (failedperc={0} cnt={1})", failPercStr, cntStr));
                return "0";
            }
        }

        /// <summary>
        /// Correct values in Jmeter CSV exported line
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private string CorrectJmeterCSVLine(string line)
        {
            //from: 01003_linkhelp,87,58,20,29,91,385,14,2445,"0,00%",",1","17,8","262,78"
            //to: 01003_linkhelp,87,58,20,29,91,385,14,2445,0.00,0.1,17.8,262.78

            //"\d+,\d+"|"\d+,\d+%"|",\d+"
            string pattern = @"""\d+,\d+""|""\d+,\d+%""|"",\d+"""; 
            string targetLine = string.Copy(line);

            // replace decimal comma with dot
            foreach (Match match in Regex.Matches(line, pattern))
            {
                string p0 = match.Value;
                string p1 = p0.Replace(',','.');
                targetLine = targetLine.Replace(p0, p1);
            }

            // remove quotes
            targetLine = targetLine.Replace("\"","");

            // correct mailformed decimal .0 notation (a,b,c,.0,d)
            targetLine = targetLine.Replace(",.",",0.");

            return targetLine;
        }

    }
}
