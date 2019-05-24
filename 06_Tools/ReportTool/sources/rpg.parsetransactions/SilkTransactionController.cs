using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using rpg.common;
using System.Text.RegularExpressions;
using System.IO;

namespace rpg.parsetransactions
{
    class SilkTransactionController: TransactionController
    {
        private const string XPATHTRSPATTERN = "//*/MeasureList/Measure[PercentileData]";
        private const string TRSNAMEGROUPPATTERNCSV = @"\;Transaction\;(.*)\;Trans\."; // alle soorten starten hiermee (Trans. failed, Trans.(busy) failed, Trans. ok, Trans.(busy) ok)
        private const string TMRNAMEGROUPPATTERNCSV = @"\;Timer\;(.*)\;Response.time";

        private const string TRSNAMEGROUPPATTERNXML = @"Transaction.*Trans.\(busy\).ok.*PercentileData"; // tbv percentieldata, die is er niet voor timers ?? (ligt bij Szabolcs)

        private const string TRSTIMINGSUBSTRING = "Trans.(busy) ok[s]";
        private const string TMRTIMINGSUBSTRING = "Response time[s]";

        private const string TRSCANCELEDSUBSTRING = "Trans. canceled[s]";
        private const string TRSFAILEDSUBSTRING = "Trans. failed[s]";
        private const string THRESHOLDCODEPOSTFIX = "_c";

        private string[] thresholdcodes = {"green", "yellow", "red"};
        private const float THRESHOLD1 = 2;
        private const float THRESHOLD2 = 3;

        private const string BRPFILETAG = "transactionfilebrp";
        private const string CSVFILETAG = "transactionfilecsv";


        //private const int NUMOFSTDTRANSACTIONS = 3; //#Overall Response Time#, Workloadexit, TInit
        private string[] stdtransactions = {"#Overall Response Time#", "Workloadexit", "TInit"};

        // constructor param chain

        public override void DoParse(ParamInterpreter parameters)
        {
            Log.WriteLine("Execute parser...");

            parameters.VerifyFileExists(BRPFILETAG);
            parameters.VerifyFileExists(CSVFILETAG);

            //SilkTransactionController c = new SilkTransactionController();
            // Read measures to in-memory intermediate format
            ReadTransactiondataFromCSV(parameters.Value(CSVFILETAG));

            // verrijken transactiedata met aggregatiedata
            EnrichTransactionDataFromCSV(parameters.Value(CSVFILETAG));
            EnrichTransactionDataFromXML(parameters.Value(BRPFILETAG));

            // controleer of bruikbare info gevonden is (alleen voor transacties is negatieve check fataal)
            if (!ContainsProjectTransactions())
                throw new Exception("No transactiondata found in testresult (tsd or brp)");
        }

        // Check is format of input files is as expected
        public override void CheckInputfileFormat(ParamInterpreter parameters)
        {
            Log.WriteLine("checking brp file...");
            string brpFilename = parameters.Value(BRPFILETAG);

            if (!CheckFormatBRPFile(brpFilename))
                throw new FormatException(brpFilename);

            Log.WriteLine("checking csv file...");
            string csvFilename = parameters.Value(CSVFILETAG);

            if (!CheckFormatCSVFile(csvFilename))
                throw new FormatException(csvFilename);
        }

        /// <summary>
        /// Check format of Silk BRP file
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private bool CheckFormatBRPFile(string fileName)
        {
            // moet xml format zijn
            // vanwaege snelheid: alleen eerste paar regels, niet hele document

            return Utils.IsXMLWithKey(fileName, "BaselineReport");
        }

        /// <summary>
        /// check if this is a CSV file
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private bool CheckFormatCSVFile(string fileName)
        {

            return Utils.IsCSVWithKey(fileName, "Transaction");
        }

        /// <summary>
        /// Extract data from CSV (converted TSD)
        /// </summary>
        /// <param name="fileName"></param>
        public void ReadTransactiondataFromCSV(string fileName)
        {
            Log.WriteLine(string.Format("read transaction data from CSV [{0}]...", fileName));
            string[] lines = File.ReadAllLines(fileName);

            // filter summary secion of the resultfile, only transaction lines
            string[] filteredLines = FilterTransactionLines(lines);

            Log.WriteLine("discover transaction names...");
            _transactionNames = ExtractTransactionNamesAllCategories(filteredLines); // transactions AND timers
            Log.WriteLine(string.Format(string.Format("{0} transactions found ({1} project transaction)", _transactionNames.Length, NumOfProjectTransactions())));
            
            Log.WriteLine("read transaction data...");
            ReadTransactionDataCSV(filteredLines, _transactionNames);
        }

        /// <summary>
        /// Extract additional data from CSV file
        /// </summary>
        /// <param name="fileName"></param>
        public void EnrichTransactionDataFromCSV(string fileName)
        {
            Log.WriteLine("enrich transaction data CSV (canceled, failed)...");
            Log.WriteLine(fileName);
            string[] lines = FilterTransactionLines( File.ReadAllLines(fileName) );

            EnrichFromCSV(lines, _transactionNames);
        } 

        /// <summary>
        /// Extract additional data from baselinereport.brp (xml)
        /// </summary>
        /// <param name="fileName"></param>
        public void EnrichTransactionDataFromXML(string fileName)
        {
            Log.WriteLine("enrich transaction data from BRP (percentiles)...");
            Log.WriteLine(fileName);
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(fileName);

            EnrichFromXML(xmlDoc, _transactionNames);
        }        

        /// <summary>
        /// Select transaction summary lines from what is a transaction from a Silk perspective (transaction, timer...)
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        private string[] FilterTransactionLines(string[] lines)
        {
            List<string> newLines = new List<string>();
            string[] splitLine;
            foreach (string line in lines)
            {   
                splitLine = line.Split(';');
                if ((splitLine.Length == 18) && ((splitLine.Contains("Transaction")) || (splitLine.Contains("Timer"))))
                {
                    newLines.Add(line);
                }
            }
            return newLines.ToArray();
        }

        /// <summary>
        /// Min, max, avg, cnt transactiondata
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="trsNames"></param>
        private void ReadTransactionDataCSV(string[] lines, string[] trsNames)
        {
            string trsName;
            foreach (string line in lines)
            {
                trsName = line.Split(';')[2];
                // levert regels voor transactions EN timers op, samen houden zolang het kan, structuur van data is hetzelfde
                if (trsNames.Contains(trsName) && (IsTransactionSuccessfulLine(line)))
                {
                    ExtractTrsDetailsCSV(line, trsName);
                }
            }
        }

        /// <summary>
        /// Failed and Canceled transactioncount
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="trsNames"></param>
        private void EnrichFromCSV(string[] lines, string[] trsNames)
        {
            bool found = false;
            string trsName;
            foreach (string line in lines)
            {
                trsName = line.Split(';')[2];
                // geldt alleen nog voor transaction regels, niet voor timers?
                if (trsNames.Contains(trsName) && (line.Contains(TRSFAILEDSUBSTRING) || line.Contains(TRSCANCELEDSUBSTRING) ))
                {
                    Log.WriteLine(trsName+" - transaction CANCELED or FAILED found in csv, extracting...");
                    ExtractAddTrsMeasureCSV(line, trsName);
                    found = true;
                }
            }
            if (!found)
                Log.WriteLine("WARNING: no canceled or failed data found for any transaction");
        }

        /// <summary>
        /// Percentile transaction data
        /// </summary>
        /// <param name="xmlDoc"></param>
        /// <param name="trsNames"></param>
        private void EnrichFromXML(XmlDocument xmlDoc, string[] trsNames)
        {
            // alleen voor transacties, niet voor Timers?? (TODO)
            XmlNodeList nodeList = xmlDoc.SelectNodes(XPATHTRSPATTERN); // zoek nodes die measures zijn met percentieldata
            string trsName;
            bool found = false;

            foreach (XmlNode node in nodeList)
            {
                trsName = node.Attributes["name"].Value;
                // uit elkaar gehaald tbv debugging
                bool isTrsLine = IsTransactionLineXML(node.InnerXml);
                bool containsTrsName = trsNames.Contains(trsName);
                if ( isTrsLine && containsTrsName ) 
                {
                    Log.WriteLine(trsName+" - transaction percentile found in xml, extracting...");
                    ExtractAddTrsDetailsXML(node.InnerXml, trsName);
                    found = true;
                }
            }
            if (!found)
                throw new Exception("No percentile data found for any transaction in BRP/XML");
        }

        private bool IsTransactionLineXML(string line)
        {
            Regex trsRegex = new Regex(TRSNAMEGROUPPATTERNXML);
            bool isMatch = trsRegex.IsMatch(line);
            return isMatch;
        }

        /// <summary>
        /// True voor Transaction- en Timer regels
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private bool IsTransactionSuccessfulLine(string line)
        {
            return ((line.Contains(TRSTIMINGSUBSTRING) || (line.Contains(TMRTIMINGSUBSTRING))));
        }

        /// <summary>
        /// Extract the results and put it into transactionDetails: cnt, min, max, avg, stdev
        /// </summary>
        /// <param name="line"></param>
        /// <param name="trsName"></param>
        private void ExtractTrsDetailsCSV(string line, string trsName)
        {
            //C;Measure group;Name;Measure type;N;Sum;Min;Max;StdDev;Avg;MinSum;MaxSum;MinMax;MaxMin;MinAvg;MaxAvg;MinCnt;MaxCnt
            //;Transaction;LoketControle_01_inloggen;Trans.(busy) ok[s];4;6,72400000;1,56000000;1,82500000;0,09698711;1,68100000;1,56000000;1,82500000;1,56000000;1,82500000;1,56000000;1,82500000;1,00000000;1,00000000
            //;Timer;DR_01_OpenDonorregister;Response time[s];1;0,80400000;0,80400000;0,80400000;0,00000000;0,80400000;0,80400000;0,80400000;0,80400000;0,80400000;0,80400000;0,80400000;1,00000000;1,00000000

            string[] parts = line.Split(';');

            // wat nog mist is toevoegen van een lege regel als alles fout gegaan is
            TransactionValue value = new TransactionValue();
            value.cnt = parts[4];
            value.min = parts[6].TrimEnd('0');
            value.avg = parts[9].TrimEnd('0');
            value.max = parts[7].TrimEnd('0');
            value.stdev = parts[8].TrimEnd('0');

            _transactionDetails.Add(trsName, value.ToString());

            Log.WriteLine(trsName + "=" + value.ToString());
        }

        /// <summary>
        /// Add measure to existing or new transaction line (count canceled trs)
        /// </summary>
        /// <param name="line"></param>
        /// <param name="trsName"></param>
        private void ExtractAddTrsMeasureCSV(string line, string trsName)
        {
            //C;Measure group;Name;Measure type;N;Sum;Min;Max;StdDev;Avg;MinSum;MaxSum;MinMax;MaxMin;MinAvg;MaxAvg;MinCnt;MaxCnt
            //;Transaction;LoketControle_01_inloggen;Trans.(busy) ok[s];4;6,72400000;1,56000000;1,82500000;0,09698711;1,68100000;1,56000000;1,82500000;1,56000000;1,82500000;1,56000000;1,82500000;1,00000000;1,00000000

            if (!_transactionDetails.items.ContainsKey(trsName))
                _transactionDetails.items.Add(trsName, TransactionValue.CreateEmptyValue());

            TransactionValue value = new TransactionValue(_transactionDetails.items[trsName]);
            string[] parts = line.Split(';');

            // failed
            if (line.Contains(TRSFAILEDSUBSTRING))
                value.fail = parts[4];

            // canceled
            if (line.Contains(TRSCANCELEDSUBSTRING))
                value.cancel = parts[4];

            _transactionDetails.items[trsName] = value.ToString();
            //Log.WriteLine("csv data extracted: "+trsName + "=" + value.ToString());
        }


        /// <summary>
        /// Extract percentile data from trs summary in bpr/xml
        /// </summary>
        /// <param name="xml"></param>
        /// <param name="trsName"></param>
        private void ExtractAddTrsDetailsXML(string xml, string trsName)
        {
            //string p90 = Regex.Match(xml, @"nr=""90""><Height>(.*?)</Height").Groups[1].Value;
            if (!_transactionDetails.items.ContainsKey(trsName))
                throw new Exception(string.Format("Transaction [{0}] not found in XML/BRP, cannot add percentile data.", trsName));

            TransactionValue value = new TransactionValue( _transactionDetails.items[trsName] );
            // 50, 90 and 99 percentiles are the most used and easily available, the rest can be calculated by interpolation
            value.median = Regex.Match(xml, @"nr=""50""><Height>(.*?)</Height").Groups[1].Value.TrimEnd('0');
            value.p90 = Regex.Match(xml, @"nr=""90""><Height>(.*?)</Height").Groups[1].Value.TrimEnd('0');
            value.p95 = Regex.Match(xml, @"nr=""95""><Height>(.*?)</Height").Groups[1].Value.TrimEnd('0');

            _transactionDetails.items[trsName] = value.ToString();
            //Log.WriteLine(trsName+" brp data extracted, 90p is : "+value.p90);
        }

        /// <summary>
        /// Collection transaction- and timer names as transactionnames
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        private string[] ExtractTransactionNamesAllCategories(string[] lines)
        {
            string[] transactionNames = ExtractTransactionNames(lines, TRSNAMEGROUPPATTERNCSV);
            string[] timerNames = ExtractTransactionNames(lines, TMRNAMEGROUPPATTERNCSV);

            Log.WriteLine(string.Format("Transaction names found: {0} ({1} transactions, {2} timers)",transactionNames.Length+timerNames.Length, transactionNames.Length, timerNames.Length));
            return transactionNames.Union(timerNames).ToArray();
        }


        public void AddThresholds(float thresholdHigh, float thresholdMax)
        {
            Log.WriteLine("Add threshold data...");

            List<string> thresholdCodes = new List<string>();
            foreach (string trsName in _transactionNames)
            {
                Log.WriteLine(trsName);
                thresholdCodes.Clear();

                foreach (string trsValue in _transactionDetails.items[trsName].Split(TransactionValue.LISTSEPARATOR))
                {
                    thresholdCodes.Add( GetThresholdCode(trsValue, thresholdHigh, thresholdMax) );
                }
                _transactionDetails.Add(trsName + THRESHOLDCODEPOSTFIX, string.Join(TransactionValue.LISTSEPARATOR.ToString(), (string[])thresholdCodes.ToArray()));
            }
        }

        /// <summary>
        /// Get value threshold evaluation as code
        /// </summary>
        /// <param name="trsValue"></param>
        /// <param name="thresholdHigh"></param>
        /// <param name="thresholdMax"></param>
        /// <returns></returns>
        private string GetThresholdCode(string trsValue, float thresholdHigh, float thresholdMax)
        {
            float trsValFloat;
            string code = thresholdcodes[0];
            if (float.TryParse(trsValue, out trsValFloat))
            {
                if (trsValFloat>=thresholdMax)
                {
                    code = thresholdcodes[2];
                }
                else if (trsValFloat>=thresholdHigh)
                {
                    code = thresholdcodes[1];
                }
            }
            return code;
        }

        /// <summary>
        /// Valideer of de gevonden transactiedata TransBusyOk transactiedata gevonden heeft, al is er maar één
        /// </summary>
        /// <returns></returns>
        public bool ContainsProjectTransactions()
        {
            // zonder trsdata staan er 3 transacties in: 
            //return (transactionDetails.items.Count > NUMOFSTDTRANSACTIONS);

            bool result = false;
            // controleer alle gevonden trsnamen, als er één tussen zit die niet std is (TInit, Overall...) dan: true
            foreach (string trsName in _transactionNames)
            {
                result = result || !stdtransactions.Contains(trsName);
            }
            return result;
        }

        /// <summary>
        /// Aantal non-standard transactienamen gevonden, dit zijn custom project transacties
        /// </summary>
        /// <returns></returns>
        public int NumOfProjectTransactions()
        {
            int result = 0;
            // controleer alle gevonden trsnamen, als er één tussen zit die niet std is (TInit, Overall...) dan: true
            foreach (string trsName in _transactionNames)
            {
                if (!stdtransactions.Contains(trsName))
                    result++;
            }
            return result;
        }
    }
}
