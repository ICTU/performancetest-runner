using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using srt.common;
using System.Text.RegularExpressions;
using System.Xml;

namespace srt.parsemeasures
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

            // read data (this can potentially blow up your memory resource!)
            //jtlTrsLines = ReadMeasureData(p.Value(TRSJTLFILETAG));

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
            _measureDetails.Add(STARTTIMEKEY, Utils.ParseJMeterEpoch(s).ToString(DATETIMETIMEFORMAT));
            
            // Interval;
            _measureDetails.Add(INTERVALKEY, JMAGGREGATEPERIOD.ToString());
        }

        /// <summary>
        /// Calculate aggregated measure data from raw sample data
        /// </summary>
        private void ExtractMeasureData(string[] lines)
        {
            //<sample t="18" it="0" lt="0" ts="1509550962840" s="true" lb="setUp: regex tbv uitwerking beoordeling" rc="200" rm="OK" tn="setUp: 05-06_gir_inspecteren 3-1" dt="text" by="59" ng="1" na="3"/>
            //<httpSample t="97" it="0" lt="96" ts="1509550966125" s="true" lb="gbr_gebruikerbeheer" rc="200" rm="OK" tn="setUp: standaardgebruikers 1-1" dt="text" by="13469" ng="1" na="1">
            Log.WriteLine("Extract measure data...");

            MeasureAggregate resptime_agg = new MeasureAggregate(); // resptime (t=time)
            MeasureAggregate errors_agg = new MeasureAggregate(); // errors (s=success)
            MeasureAggregate numofthreads_agg = new MeasureAggregate(); // number of active threads (na)

            long threshold = 0;
            long timestamp = 0;
            long valueCnt = 0;
            long aggregateCnt = 0;
            long timespan = 0;

            // build aggregated timeseries
            foreach (string line in lines)
            {
                if (IsSeriesLine(line))
                {
                    // extract transactiedata naar aggregatie objecten
                    try // gevoelig stuk
                    {
                        string[] values = JmeterLine.Parse(line);
                        valueCnt++;

                        // ts = timestamp
                        timestamp = long.Parse(values[1]);

                        // optellen voor de aggregatieperiode
                        // t = response time (int milliseconds)
                        resptime_agg.Add(int.Parse(values[2]));
                        // errors
                        errors_agg.Add(bool.Parse(values[3]) ? 0 : 1); // if success: err=0, else: 1
                        //concurrent threads
                        numofthreads_agg.Add(int.Parse(values[4])); 
                    }
                    catch (Exception e)
                    {
                        throw new FormatException(String.Format("unexpected format of data found in line:\r\n [{0}]\r\n {1}", line, e.Message));
                    }

                    // TODO add individual transaction measures

                    timespan = timestamp - threshold;

                    // eens per periode aggregatie berekenen en door met vlg aggr blok
                    if (timespan > JMAGGREGATEPERIOD)
                    {
                        // agg -> measuredetails
                        _measureDetails.Add(OVERALLRESPONSETIMEKEY, Utils.NormalizeTime( resptime_agg.Avg().ToString() )); // normalize
                        _measureDetails.Add(OVERALLTRANSACTIONSKEY, resptime_agg.Count().ToString());
                        _measureDetails.Add(OVERALLUSERSKEY, numofthreads_agg.Max().ToString());
                        _measureDetails.Add(OVERALLERRORSKEY, errors_agg.Sum().ToString());

                        // reset all
                        resptime_agg.Reset();
                        errors_agg.Reset();
                        numofthreads_agg.Reset();

                        threshold = timestamp;
                        aggregateCnt++;
                    }
                }
            }

            // only collect rest aggregatoin data if samples are added
            if (resptime_agg.Count() > 0)
            {
                // agg -> measuredetails (laaste restje wordt toegevoegd)
                _measureDetails.Add(OVERALLRESPONSETIMEKEY, Utils.NormalizeTime(resptime_agg.Avg().ToString())); // normalize
                _measureDetails.Add(OVERALLTRANSACTIONSKEY, resptime_agg.Count().ToString());
                _measureDetails.Add(OVERALLUSERSKEY, numofthreads_agg.Max().ToString());
                _measureDetails.Add(OVERALLERRORSKEY, errors_agg.Sum().ToString());
            }

            // fill measurenames (only timeseries)
            List<string> names = new List<string>();
            names.Add(OVERALLRESPONSETIMEKEY);
            names.Add(OVERALLTRANSACTIONSKEY);
            names.Add(OVERALLUSERSKEY);
            names.Add(OVERALLERRORSKEY);

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
        /// Read measures from jtl: ts= lb=|x| t= na= s= 
        /// </summary>
        /// <param name="jtlFileName"></param>
        private string[] ReadMeasureDataText(string jtlFileName)
        {
            Log.WriteLine(string.Format("read measure data (as text) from {0}...", jtlFileName));

            string[] inLines = ReadLinesFromFile(jtlFileName, "ample t=");
            List<string> outLines = new List<string>();
            Regex regex = new Regex("t=\"(\\d+)\".+ts=\"(\\d+)\".+s=\"(\\w+)\".+lb=\"(.+)\".rc.+na=\"(\\d+)\"");

            int cnt = 0;

            foreach (string line in inLines)
            {
                cnt++;
                //if (cnt<10)
                //    Log.WriteLine("DEBUG line="+ line);

                Match match = regex.Match(line);

                if (match.Success)
                {
                    string outLine = string.Format("ts={0} t={1} na={2} s={3} lb={4}",
                        match.Groups[2].Value, // ts
                        match.Groups[1].Value, // t
                        match.Groups[5].Value, // na
                        match.Groups[3].Value, // s
                        match.Groups[4].Value); // lb

                    outLines.Add(outLine);

                    //if (cnt<10)
                    //    Log.WriteLine("DEBUG out="+ outLine);
                }
            }

            Log.WriteLine( string.Format("{0} lines out of {1} selected for evaluation", outLines.Count, inLines.Length) );
            return outLines.ToArray();
        }

    }
}
