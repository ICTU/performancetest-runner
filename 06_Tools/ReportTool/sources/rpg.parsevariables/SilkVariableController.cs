using System;
using rpg.common;
using System.Globalization;

namespace rpg.parsevariables
{
    class SilkVariableController: VariableController
    {
        //Log.WriteLine("silkperformer: <in:transactionfilecsv> <in:infilebrp>");
        string[] csvLines;
        string[] brpLines;
        //private const string PRINTABLETMEPATTERN = @"\;Time\:\;(.*)\;\[";
        //private const string PRINTABLETMEPATTERN = @"Summary General\;---\;Active users\;([0-9:.\s]+)\;\d\;";
        private const string PRINTABLETMEPATTERN = @"Summary General\;---\;Transactions\;([0-9:.\s]+)\;\d\;";

        private const string MERGEDUSERSPATTERN = @"Merged users\:\;(\d+)";
        private const string OVERALLTRANSFAILEDPATTERN = @"Transaction\;#Overall Response Time#\;Trans\. failed\[s\]\;(\d+)";
        private const string OVERALLTRANSOKPATTERN = @"Transaction\;#Overall Response Time#\;Trans. ok\[s\];(\d+)";

        private const string TRANSACTIONFILEBRPNAME = "transactionfilebrp";
        private const string TRANSACTIONFILECSV = "transactionfilecsv";

        public override string ParseLoadgenType()
        {
            Log.WriteLine("parse loadgentype...");
            return "Silkperformer";
        }

        public override void ReadLinesFromFile(ParamInterpreter p)
        {
            csvLines = ReadLinesFromFile(p.Value(TRANSACTIONFILECSV));
            brpLines = ReadLinesFromFile(p.Value(TRANSACTIONFILEBRPNAME));
        }

        /// <summary>
        /// Timing variables
        /// </summary>
        public override string ParsePrintableTime()
        {
            //2014.7.1.14.53.9
            Log.WriteLine("parse printabletime...");

            string val = Utils.ExtractValueByPatternFirst(csvLines, PRINTABLETMEPATTERN);
            Log.WriteLine("raw value found: "+val);

            val = val.Replace(' ','.');
            val = val.Replace(':','.');
            string[] values = val.Split('.');

            DateTime dt = new DateTime(int.Parse(values[0]), int.Parse(values[1]), int.Parse(values[2]), int.Parse(values[3]), int.Parse(values[4]), int.Parse(values[5]));
            string returnValue = dt.ToString(DATETIMEPTFORMAT);

            return returnValue;
        }

        /// <summary>
        /// Parse test duration in seconds
        /// </summary>
        /// <returns></returns>
        public override string ParseTestDuration(string reference)
        {
            Log.WriteLine("calculate testduration...");

            string diffValueStr;
            try
            {
                string val = Utils.ExtractValueByPatternLast(csvLines, PRINTABLETMEPATTERN);
                //Log.WriteLine("test end-time found: "+val);
                val = val.Replace(' ', '.');
                val = val.Replace(':', '.');
                string[] values = val.Split('.');
                DateTime lastValue = new DateTime(int.Parse(values[0]), int.Parse(values[1]), int.Parse(values[2]), int.Parse(values[3]), int.Parse(values[4]), int.Parse(values[5]));

                DateTime referenceValue;
                DateTime.TryParseExact(reference, DATETIMEPTFORMAT, CultureInfo.InvariantCulture, DateTimeStyles.None, out referenceValue);

                diffValueStr = (lastValue - referenceValue).ToString(DURATIONTIMEFORMAT);
            }
            catch (Exception)
            {
                diffValueStr = "0";
            }

            return diffValueStr;
        }

        /// <summary>
        /// Extract Merged users variable from silk csv
        /// </summary>
        public override string ParseMergedUsers()
        {
            Log.WriteLine("parse mergedusers...");

            string value = Utils.ExtractValueByPatternFirst(csvLines, MERGEDUSERSPATTERN);

            Log.WriteLine("mergedusers="+value);
            return value;
        }

        /// <summary>
        /// Calculate fault percentage from silk csv
        /// </summary>
        /// <returns></returns>
        public override string ParseFaultPercentage()
        {
            Log.WriteLine("calculate faultpercentage...");

            double faultPercentage = 0;
            try
            {
                string transok = Utils.ExtractValueByPatternFirst(csvLines, OVERALLTRANSOKPATTERN);
                string transfail = Utils.ExtractValueByPatternFirst(csvLines, OVERALLTRANSFAILEDPATTERN);
                faultPercentage = 100 * Int32.Parse(transfail) / (Int32.Parse(transfail) + Int32.Parse(transok));

                Log.WriteLine("transok="+transok);
                Log.WriteLine("transfail="+transfail);
                Log.WriteLine("faultpercentage="+faultPercentage.ToString());
            }
            catch { }

            return faultPercentage.ToString("0.00").Replace(",",".");
        }

    }
}
