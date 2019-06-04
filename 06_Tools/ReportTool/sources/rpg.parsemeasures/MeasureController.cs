using System;
using System.Collections.Generic;
using rpg.common;
using System.IO;

namespace rpg.parsemeasures
{
    abstract class MeasureController
    {
        public MeasureDetails _measureDetails = new MeasureDetails();
        public string[] _measureNames; // measure name tags
        public Intermediate _variables = new Intermediate();

        public const string STARTTIMEKEY = "Time";
        public const string DATETIMETIMEFORMAT = "yyyy,M,d,H,m,s"; // Time output format (highcharts)
        public const string INTERVALKEY = "Interval";

        // names = silk names
        public const string OVERALLRESPONSETIMEKEY = "Transaction#Overall_Response_Time#Trans.(busy)_ok[s]"; 
        public const string OVERALLTRANSACTIONSKEY = "Summary_General---Transactions";
        public const string OVERALLERRORSKEY = "Summary_General---Errors";
        public const string OVERALLUSERSKEY = "Summary_General---Active_users";
        public const string OVERALLTIMESERIESKEY = "measuretimeseries";

        public const string TRENDBREAKSTABILITYPRCKEY = "trendbreakstabilitypercentage";
        public const string TRENDBREAKRAMPUPPRCKEY = "trendbreakrampuppercentage";
        public const string TRENDBREAKRAMPUPUSRKEY = "trendbreakrampupusers";

        public const string REPORTTRANSACTIONNAMEPATTERN = @"\d\d_"; // TODO naar app config

        public void Parse(ParamInterpreter p)
        {
            // from here: technology specific, handled in overrides in derived classes

            // check input file format (is it what we expect?)
            try
            {
                CheckInputfileFormat(p);
            }
            catch (Exception e)
            {
                throw new FormatException("Cannot parse input file, format problem: "+e.Message);
            }

            // perform loadgen specific parse (in derived classes)
            DoParse(p);

            // convert data to confom system locale settings (output formatting is done during merge)
            FormatData();

            // from here: generic for all technology, handled on parent level

            // perform post-processing on parsed measure data (derived data and trend analysis)
            Postprocess();

            // Write intermediate file
            WriteIntermediate(p);
        }

        // Implemented by derived classes (standard returns false to force override)
        public abstract void CheckInputfileFormat(ParamInterpreter parameters);

        public abstract void DoParse(ParamInterpreter p);

        public abstract void FormatData();

        /// <summary>
        /// Definitions and measures -> 1 file
        /// </summary>
        /// <param name="filename"></param>
        public void WriteIntermediate(ParamInterpreter p)
        {
            Log.WriteLine("write intermediate data...");

            string fileName = p.Value("intermediatefile");
            Log.WriteLine("measure data to "+fileName);
            _measureDetails.items.WriteToFile(fileName);

            fileName = p.Value("intermediatefilevars");
            Log.WriteLine("measure data vars to " + fileName);
            _variables.WriteToFile(fileName);
        }

        /// <summary>
        /// Read values from csv source
        /// </summary>
        /// <param name="fileName"></param>
        public string[] ReadLinesFromFile(string fileName)
        {
            return ReadLinesFromFile(fileName, null);
        }

        /// <summary>
        /// Read values from text source with filter
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="hint"></param>
        /// <returns></returns>
        public string[] ReadLinesFromFile(string fileName, string filter)
        {
            Log.WriteLine("read lines from file...");
            Log.WriteLine(fileName);

            string line;
            List<string> lines = new List<string>();
            int cnt = 0;

            StreamReader sr = new StreamReader(fileName);
            while ((line = sr.ReadLine()) != null)
            {
                cnt++;
                // no filter: evaluate all lines
                if (filter == null)
                {
                    lines.Add(line);
                }
                // if filter: apply filter
                else
                {
                    if (line.Contains(filter))
                        lines.Add(line);
                }
            }
            sr.Close();

            Log.WriteLine(string.Format("read={0} selected={1}", cnt, lines.Count));
            //File.ReadAllLines(fileName);

            return lines.ToArray();
        }

        /// <summary>
        /// Post-processing, extract extra data from parsed measures (time series)
        /// </summary>
        public void Postprocess()
        {
            // determine test time progression on 
            Log.WriteLine("post-processing on collected measure aggregates...");

            ExtractRampupBreak();
            ExtractStabilityBreak();
        }

        /// <summary>
        /// Identify break in ramp up trend
        /// </summary>
        private void ExtractRampupBreak()
        {
            Log.WriteLine("calculating ramp up break on [" + OVERALLTRANSACTIONSKEY + "] by reference [" + OVERALLUSERSKEY + "]");

            try
            {
                // get timeseries
                double[] throughput_values = _measureDetails.GetValuesAsDoubleArray(OVERALLTRANSACTIONSKEY);
                double[] users_values = _measureDetails.GetValuesAsDoubleArray(OVERALLUSERSKEY);

                TrendAnalyzer trendAnalyzer = new TrendAnalyzer();
                trendAnalyzer.ReferenceSeries = users_values;

                // search for trend break if stable ramp-up is expected (stress testing)
                trendAnalyzer.DetectTrendBreak_Rampup(throughput_values);

                _variables.Add(TRENDBREAKRAMPUPPRCKEY, trendAnalyzer.GetBreakPercentage_Reference().ToString("0"));
                _variables.Add(TRENDBREAKRAMPUPUSRKEY, trendAnalyzer.GetBreakReferenceValue().ToString());

                Log.WriteLine(string.Format("trend break RAMP UP: break on percentage={0:0}% users={1}", trendAnalyzer.GetBreakPercentage_Reference(), trendAnalyzer.GetBreakReferenceValue()));
            }
            catch (Exception e)
            {
                Log.WriteLine("ERROR calculation of ramp-up trend break failed");
                Log.WriteLine(e.Message);

            }
        }

        /// <summary>
        /// Identify break in stability trend
        /// </summary>
        private void ExtractStabilityBreak()
        {
            Log.WriteLine("calculating stability break on [" + OVERALLTRANSACTIONSKEY + "] by reference [" + OVERALLUSERSKEY + "]");

            try
            {
                // get timeseries
                double[] throughput_values = _measureDetails.GetValuesAsDoubleArray(OVERALLTRANSACTIONSKEY);
                double[] users_values = _measureDetails.GetValuesAsDoubleArray(OVERALLUSERSKEY);
                double[] error_values = _measureDetails.GetValuesAsDoubleArray(OVERALLERRORSKEY);

                TrendAnalyzer trendAnalyzer = new TrendAnalyzer();
                trendAnalyzer.ReferenceSeries = users_values;

                // search for trendbreak if stability is expected (duration testing)
                Log.WriteLine("phase 1: stability of throughput");
                int breakThroughput = trendAnalyzer.DetectTrendBreak_Stability(throughput_values);
                Log.WriteLine("phase 2: stability of errors");
                int breakErrors = trendAnalyzer.DetectTrendBreak_Stability(error_values);

                // break is lowest break level
                int breakResult = (breakThroughput < breakErrors) ? breakThroughput : breakErrors;
                trendAnalyzer.BreakIndex = breakResult;

                _variables.Add(TRENDBREAKSTABILITYPRCKEY, trendAnalyzer.GetBreakPercentage_Progress().ToString("0"));

                Log.WriteLine(string.Format("trend break STABILITY: break on percentage={0:0}%", trendAnalyzer.GetBreakPercentage_Progress()));
            }
            catch (Exception e)
            {
                Log.WriteLine("ERROR calculation of stability trend break failed");
                Log.WriteLine(e.Message);

            }
        }

    }
}
