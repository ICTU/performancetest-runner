using rpg.common;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;

namespace rpg.parsemeasures
{
    class JmeterMeasureController : MeasureController
    {
        string[] jtlTrsLines;

        const int JMAGGREGATEPERIOD = 10000; // ms interval (10.000=10s)
        
        const string TRSJTLFILETAG = "transactionfilejtl";
        const string JTLCHECKSTR = "testResults";

        /// <summary>
        /// JMeter measure parser, toplevel executor
        /// </summary>
        /// <param name="p"></param>
        public override void DoParse(ParamInterpreter p)
        {
            // inlezen bruikbare jtl regels
            p.VerifyFileExists(TRSJTLFILETAG);
            jtlTrsLines = ReadMeasureDataText(p.Value(TRSJTLFILETAG));

            // ruwe measure data aggregeren
            ExtractMeasureData(jtlTrsLines);

            // additionele definities (nodig voor config van grafieken) inlezen -> measuredetails
            ExtractAdditionals(jtlTrsLines);
        }

        // Implemented by derived classes (standard returns false to force override)
        public override void CheckInputfileFormat(ParamInterpreter parameters)
        {
            string filename = parameters.Value(TRSJTLFILETAG);

            Log.WriteLine("checking file format (XML + keyvalue) of " + filename);

            if (!Utils.IsXMLWithKey(filename, JTLCHECKSTR))
                throw new FormatException("file is not XML format, or missing crucial key values: " + filename);
        }

        /// <summary>
        /// Extract additional info from jtl
        /// </summary>
        /// <param name="jtlTrsLines"></param>
        private void ExtractAdditionals(string[] jtlTrsLines)
        {
            // Time
            string s = Utils.ExtractValueByPatternFirst(jtlTrsLines, @"ts=(\d+)");
            _variables.Add(STARTTIMEKEY, Utils.ParseJMeterEpoch(s).ToString(DATETIMETIMEFORMAT));

            // Interval;
            _variables.Add(INTERVALKEY, JMAGGREGATEPERIOD.ToString());
        }

        /// <summary>
        /// Calculate aggregated measure data from raw sample data
        /// </summary>
        private void ExtractMeasureData(string[] lines)
        {
            //<sample t="18" it="0" lt="0" ts="1509550962840" s="true" lb="setUp: regex tbv uitwerking beoordeling" rc="200" rm="OK" tn="setUp: 05-06_gir_inspecteren 3-1" dt="text" by="59" ng="1" na="3"/>
            //<httpSample t="97" it="0" lt="96" ts="1509550966125" s="true" lb="gbr_gebruikerbeheer" rc="200" rm="OK" tn="setUp: standaardgebruikers 1-1" dt="text" by="13469" ng="1" na="1">
            Log.WriteLine(string.Format("Extract measure data ({0} lines)...", lines.Length));

            MeasureAggregate resptime_agg = new MeasureAggregate(); // resptime (t=time)
            MeasureAggregate errors_agg = new MeasureAggregate(); // errors (s=success)
            MeasureAggregate numofthreads_agg = new MeasureAggregate(); // number of active threads (na)

            long threshold = 0;
            long timestamp = 0;
            long valueCnt = 0;
            long aggregateCnt = 0;
            long timespan = 0;
            long timeSeriesPoint = 0;


            // Sort the list of lines based on ts (should always be the first field)
            Array.Sort(lines);

            // build aggregated timeseries
            foreach (string line in lines)
            {
                //Log.WriteLine("DEBUG processing line: " + line);
                if (IsSeriesLine(line))
                {
                    //Log.WriteLine("DEBUG is series line, processing: " + line);
                    // extract transactiedata naar aggregatie objecten
                    try // gevoelig stuk
                    {
                        Dictionary<string, string> values = JmeterLineClean.Parse(line);
                        valueCnt++;

                        //Log.WriteLine("DEBUG gaat parsen naar long: "+ values["ts"]);
                        // ts = timestamp
                        timestamp = long.Parse(values["ts"]);
                        
                        // t = response time (int milliseconds)
                        resptime_agg.Add(int.Parse(values["t"])); // optellen voor de aggregatieperiode

                        // errors
                        errors_agg.Add(bool.Parse(values["s"]) ? 0 : 1); // if success: err=0, else: 1

                        //concurrent threads
                        numofthreads_agg.Add(int.Parse(values["na"]));
                    }
                    catch (Exception e)
                    {
                        throw new FormatException(String.Format("unexpected format of data found in line:\r\n [{0}]\r\n {1}", line, e.Message));
                    }

                    // TODO add individual transaction measures
                    if (threshold == 0 ) { threshold = timestamp; } // set it for the first time to prevent large timejump
                    timespan = timestamp - threshold;

                    // eens per periode aggregatie berekenen en door met vlg aggr blok, als timespan negatief is dan klopt de sortering in het JTL bestand niet
                    if (timespan > JMAGGREGATEPERIOD || timespan < 0)
                    {
                        for (int a = 0; a < (timespan / JMAGGREGATEPERIOD); ++a) //for each timebucket create a datapoint
                        {
                            // agg -> measuredetails
                            _measureDetails.Add(OVERALLRESPONSETIMEKEY, Utils.jmeterTimeToIntermediateSecondsString(resptime_agg.Avg().ToString())); // normalize
                            _measureDetails.Add(OVERALLTRANSACTIONSKEY, resptime_agg.Count().ToString());
                            _measureDetails.Add(OVERALLUSERSKEY, numofthreads_agg.Max().ToString());
                            _measureDetails.Add(OVERALLERRORSKEY, errors_agg.Sum().ToString());

                            // add timeseries sequence (in seconds)
                            timeSeriesPoint = (int)aggregateCnt * (JMAGGREGATEPERIOD / 1000); // timeseries datapoint in seconds
                            _measureDetails.Add(OVERALLTIMESERIESKEY, timeSeriesPoint.ToString());

                            // reset all
                            resptime_agg.Reset();
                            errors_agg.Reset();
                            numofthreads_agg.Reset();

                            threshold = timestamp;
                            aggregateCnt++;
                        }
                    }
                }
            }

            // if no matching transaction found: exit with readable message
            // TODO move this check to level up (to have all types of logs comply)
            if (valueCnt == 0)
            {
                string msg = "no transaction names found that meet naming convention (" + REPORTTRANSACTIONNAMEPATTERN + ")";
                Log.WriteLine("SEVERE: " + msg);
                throw new Exception(msg);
            }

            // only collect rest aggregation data if samples are added
            if (resptime_agg.Count() > 0)
            {
                // agg -> measuredetails (laaste restje wordt toegevoegd)
                _measureDetails.Add(OVERALLRESPONSETIMEKEY, Utils.jmeterTimeToIntermediateSecondsString(resptime_agg.Avg().ToString())); // normalize
                _measureDetails.Add(OVERALLTRANSACTIONSKEY, resptime_agg.Count().ToString());
                _measureDetails.Add(OVERALLUSERSKEY, numofthreads_agg.Max().ToString());
                _measureDetails.Add(OVERALLERRORSKEY, errors_agg.Sum().ToString());

                // add timeseries sequence (in seconds)
                timeSeriesPoint = (int)aggregateCnt * (JMAGGREGATEPERIOD / 1000); // timeseries datapoint in seconds
                _measureDetails.Add(OVERALLTIMESERIESKEY, timeSeriesPoint.ToString());

            }

            // fill measurenames (only timeseries)
            List<string> names = new List<string>();
            names.Add(OVERALLRESPONSETIMEKEY);
            names.Add(OVERALLTRANSACTIONSKEY);
            names.Add(OVERALLUSERSKEY);
            names.Add(OVERALLERRORSKEY);
            names.Add(OVERALLTIMESERIESKEY);

            _measureNames = names.ToArray();

            Log.WriteLine(string.Format("raw values: {0} aggregated: {1}", valueCnt, aggregateCnt));
        }

        /// <summary>
        /// Formatting of Jmeter data (decimal AND field separator)
        /// </summary>
        public override void FormatData()
        {
            Log.WriteLine("Format data...");

            // convert standard separators with custom ones
            _measureDetails.FormatMeasureData(_measureNames, ','); // src decimal separator not applicable for jmeter
        }

        /// <summary>
        /// Check if line contains seriesdata
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private bool IsSeriesLine(string line)
        {
            // series lines already collected
            return Regex.IsMatch(line, REPORTTRANSACTIONNAMEPATTERN);
                
    }

        /// <summary>
        /// Read measures from jtl: ts= lb=|x| t= na= s= 
        /// </summary>
        /// <param name="jtlFileName"></param>
        private string[] ReadMeasureDataXML(string jtlFileName) // deprecated, caused memory problems with large jtl
        {
            Log.WriteLine(string.Format("read measure data (as xml) from {0}...", jtlFileName));

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(jtlFileName);

            XmlNode rootNode = xmlDoc.SelectSingleNode("/testResults");

            List<string> lines = new List<string>();
            foreach (XmlNode node in rootNode.ChildNodes) // deze scant niet subtransacties neem ik aan?
            {
                lines.Add(string.Format("ts={3} t={2} na={0} s={1} lb={4}", node.Attributes["na"].Value, node.Attributes["s"].Value, node.Attributes["t"].Value, node.Attributes["ts"].Value, node.Attributes["lb"].Value));
            }

            Log.WriteLine(lines.Count + " lines selected for evaluation");
            return lines.ToArray();
        }

        /// <summary>
        /// Read measures from jtl to clean line format: ts= lb=|x| t= na= s= 
        /// </summary>
        /// <param name="jtlFileName"></param>
        private string[] ReadMeasureDataText(string jtlFileName)
        {
            Log.WriteLine(string.Format("read measure data (as text) from {0}...", jtlFileName));

            //string[] inLines = ReadLinesFromFile(jtlFileName, "ample t="); // gevaarlijk (volgorde), TODO
            string[] inLines = ReadLinesFromFile(jtlFileName, JmeterLineRaw.SamplePattern);
            List<string> outLines = new List<string>();

            // TODO dit is erg gevoelig, IES struikelt hierover. beter checks op aanwezigheid attributes anders doen
            // zodat ontbreken van tags en volgorde niet belangrijk zijn
            //Regex regex = new Regex("t=\"(\\d+)\".+ts=\"(\\d+)\".+s=\"(\\w+)\".+lb=\"(.+)\".rc.+na=\"(\\d+)\"");

            int cnt = 0;

            foreach (string line in inLines)
            {
                if (JmeterLineRaw.IsUsableLine(line))
                {
                    cnt++;
                    if (cnt < 10)
                        Log.WriteLine("first 10 input: " + line);

                    Dictionary<string, string> attributes = JmeterLineRaw.GetSampleAttributes(line);

                    string outLine = string.Format("ts={0} t={1} na={2} s={3} lb={4}",
                        attributes["ts"],
                        attributes["t"],
                        attributes["na"],
                        attributes["s"],
                        attributes["lb"]);

                    outLines.Add(outLine);

                    if (cnt < 10)
                        Log.WriteLine("1st 10 extraction: " + outLine);
                }
                else
                {
                    Log.WriteLine("WARNING useless jtl line skipped: " + line);
                }
            }

            Log.WriteLine( string.Format("{0} lines out of {1} selected for evaluation", outLines.Count, inLines.Length) );
            return outLines.ToArray();
        }

    }
}
