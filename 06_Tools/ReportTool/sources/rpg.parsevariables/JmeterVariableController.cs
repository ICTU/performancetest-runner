using System;
using rpg.common;
using System.Globalization;

namespace rpg.parsevariables
{
    class JmeterVariableController: VariableController
    {
        private const string FAULTPERCENTAGEPATTERN = @"TOTAL.+""(\d+[,.]\d+)%"; //@"TOTAL.*""(\d+,\d+)%";
        private const string TESTENDTIMEPATTERN = @"ts=""(\d+)";
        private const string TESTSTARTTIMEPATTERN = @"ts=""(\d+)";
        private const string MERGEDUSERSPATTERN = @"na=""(\d+)""";

        private const string TRANSACTIONIFILEJTL = "transactionfilejtl";
        private const string TRANSACTIONFILECSV = "transactionfilecsv";

        private string[] jtlLines;
        private string[] csvLines;

        public override string ParseLoadgenType()
        {
            Log.WriteLine("parse loadgentype...");
            return "JMeter";
        }

        public override void ReadLinesFromFile(ParamInterpreter p)
        {
            jtlLines = ReadLinesFromFileJTL(p.Value(TRANSACTIONIFILEJTL));
            csvLines = ReadLinesFromFile(p.Value(TRANSACTIONFILECSV));
        }

        /// <summary>
        /// na="117"
        /// </summary>
        public override string ParseMergedUsers()
        {
            Log.WriteLine("parse mergedusers...");
            return ExtractMaxValueByPattern(jtlLines, MERGEDUSERSPATTERN);
        }

        /// <summary>
        /// in: ts="1509550962840"
        /// out: "2014.7.1.14.53.9"
        /// </summary>
        public override string ParsePrintableTime()
        {
            Log.WriteLine("parse printabletime...");

            // first sample from jtl has ts= (timestamp)
            //string grabValue = Utils.ExtractValueByPatternFirst(jtlLines, TESTSTARTTIMEPATTERN); // first is test starttime
            string grabValue = Utils.ExtractValueByPatternLowest(jtlLines, TESTSTARTTIMEPATTERN); // lowest is test starttime

            string returnValue = Utils.ParseJMeterEpoch(grabValue).ToString(DATETIMEPTFORMAT);

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
                // first sample from jtl has ts= (timestamp)
                //string grabValue = Utils.ExtractValueByPatternLast(jtlLines, TESTENDTIMEPATTERN); // last is test eind time
                string grabValue = Utils.ExtractValueByPatternHighest(jtlLines, TESTENDTIMEPATTERN); // highest is test eind time
                DateTime lastValue = Utils.ParseJMeterEpoch(grabValue);
                Log.WriteLine("test end-time found: " + lastValue);

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
        /// Parse faultpercentage from csv
        /// </summary>
        /// <returns></returns>
        public override string ParseFaultPercentage()
        {
            Log.WriteLine("parse faultpercentage...");
            // 10th position of
            //TOTAL,82726,314,90,634,1122,2277,0,435097,"0,25%","22,8","940,6","3480,81"
            string val = "0";
            try
            {
                // dit kan mis gaan afh van regional settings van werkstation waarop conversie naar CSV wordt gedaan (. of , als decimalseparator en als deze hetzelfde is als fieldseparator worden dubbelequotes toegevoegd)
                // voor nu regex pattern ongevoelig gemaakt voor aanwezigheid van " en van . of ,
                val = Utils.ExtractValueByPatternLast(csvLines, FAULTPERCENTAGEPATTERN);
            }
            catch { }

            return Utils.ToMeasureFormat(val); //val.Replace(",", ".");
        }

    }
}
