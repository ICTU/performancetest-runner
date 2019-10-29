using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using rpg.common;

namespace rpg.parsevariables
{
    abstract class VariableController
    {
        //public Dictionary<string,string> variables = new Dictionary<string,string>();
        public Intermediate _variables = new Intermediate();

        public const string RPTGENDATEKEY = "rptgendatetime0"; // eerste generated report (rptgendatetime in rapport = dtm laatste rpt)
        public const string PRINTABLETIMEKEY = "printableTime";
        public const string TESTRUNDATETIMEKEY = "testrundatetime";
        public const string MEREGEDUSERSKEY = "mergedusers";
        public const string LGTYPEKEY = "loadgeneratortype";
        public const string TESTDURATIONKEY = "testduration";
        public const string FAULTPERCENTAGEKEY = "faultpercentage";

        public const string DATETIMEPTFORMAT = "yyyy.MM.dd.HH.mm.ss"; // printableTime output format with leading 0 and . separator
        public const string DURATIONTIMEFORMAT = @"hh\:mm\:ss"; // time format (testrun duration)

        public void Parse(ParamInterpreter p)
        {
            ReadLinesFromFile(p);

            // rptgendatetime, no data needed
            ParseRptGenDateTime();

            _variables.Add(PRINTABLETIMEKEY, ParsePrintableTime()); // for backward compatibility
            Log.WriteLine(PRINTABLETIMEKEY+"="+_variables[PRINTABLETIMEKEY]);

            _variables.Add(TESTRUNDATETIMEKEY, _variables[PRINTABLETIMEKEY]); // duplicate PrintableTime to proper variable name
            Log.WriteLine(TESTRUNDATETIMEKEY + "=" + _variables[TESTRUNDATETIMEKEY]);

            _variables.Add(TESTDURATIONKEY, ParseTestDuration(_variables[TESTRUNDATETIMEKEY]));
            Log.WriteLine(TESTDURATIONKEY + "=" + _variables[TESTDURATIONKEY]);

            _variables.Add(MEREGEDUSERSKEY, ParseMergedUsers());
            Log.WriteLine(MEREGEDUSERSKEY + "=" + _variables[MEREGEDUSERSKEY]);

            _variables.Add(LGTYPEKEY, ParseLoadgenType());
            Log.WriteLine(LGTYPEKEY + "=" + _variables[LGTYPEKEY]);

            _variables.Add(FAULTPERCENTAGEKEY, ParseFaultPercentage());
            Log.WriteLine(FAULTPERCENTAGEKEY + "=" + _variables[FAULTPERCENTAGEKEY]);

            WriteIntermediate( p.Value("intermediatefile") );
        }

        public abstract string ParsePrintableTime();

        public abstract string ParseMergedUsers();

        public abstract string ParseLoadgenType();

        public abstract void ReadLinesFromFile(ParamInterpreter p);

        public abstract string ParseTestDuration(string reference);

        public abstract string ParseFaultPercentage();

        /// <summary>
        /// Current date/time
        /// </summary>
        private void ParseRptGenDateTime()
        {
            string v = DateTime.Now.ToString(DATETIMEPTFORMAT);

            Log.WriteLine(RPTGENDATEKEY + "=" + v);
            _variables.Add(RPTGENDATEKEY, v);
        }

        /// <summary>
        /// Read values from csv source and dual grouped regex
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public string[] ReadLinesFromFile(string fileName)
        {
            Log.WriteLine("read lines from file...");
            Log.WriteLine(fileName);
            string[] lines = File.ReadAllLines(fileName);
            return lines;
        }

        /// <summary>
        /// Overload ReadLinesFromFile, but filter JTL lines on validity
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public string[] ReadLinesFromFileJTL(string fileName)
        {
            string[] orgLines = ReadLinesFromFile(fileName);

            Log.WriteLine("filter only usable JTL lines...");
            List<string> validLines = new List<string>();

            foreach (string orgLine in orgLines)
            {
                if (JmeterLineRaw.IsUsableLine(orgLine))
                    validLines.Add(orgLine);
            }
            return validLines.ToArray();
        }

        /// <summary>
        /// Extract variable=value from source
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="varName"></param>
        /// <param name="valuePattern"></param>
        /// <returns></returns>
        public bool ExtractValueByPatternMultiple(string[] lines, string varName, string valuePattern)
        {
            bool result = false;
            Regex regex = new Regex(valuePattern);
            foreach (string line in lines)
            {
                if (regex.IsMatch(line))
                {
                    foreach (Match match in regex.Matches(line))
                    {
                        _variables.Add(varName, match.Value);
                        Log.WriteLine("found: " + varName + "=" + match.Value);
                        result = true;
                    }
                }
            }
            return result;
        }

        public string ExtractMaxValueByPattern(string[] lines, string valuePattern)
        {
            int max = 0;
            Regex regex = new Regex(valuePattern);
            foreach (string line in lines)
            {
                if (regex.IsMatch(line))
                {
                    string val = regex.Match(line).Groups[1].Value;
                    int i = int.Parse(val);
                    if (i > max)
                        max = i;
                }
            }
            return max.ToString();
        }

        /// <summary>
        /// Write the vaiable(s) in intermediate format
        /// </summary>
        /// <param name="fileName"></param>
        public void WriteIntermediate(string fileName)
        {
            Log.WriteLine("Writing intermediate...");
            Log.WriteLine(fileName);

            using (StreamWriter sw = new StreamWriter(fileName))
            {
                foreach (KeyValuePair<string, string> variable in _variables)
                {
                    sw.WriteLine(variable.Key + Intermediate.KEYVALUESEPARATOR + variable.Value);
                    //Log.WriteLine(variable.Key);
                }
            }
        }
    }
}
