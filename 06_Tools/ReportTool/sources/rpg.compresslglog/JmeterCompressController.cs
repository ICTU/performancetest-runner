using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using rpg.common;

namespace rpg.compresslglog
{
    class JmeterCompressController: CompressController
    {
        private const string JMXMON_TAG = "kg.apc.jmeter.jmxmon.JMXMonSampleResult";
        private StreamWriter outputStream;
        private int srcLinesCount = 0;
        private int destLinesCount = 0;
        public const string JTLFILESRC_TAG = "transactionfilejtl";
        public const string DESTFILE_TAG = "destinationfile";

        public override void Compress(ParamInterpreter parameters)
        {
            parameters.VerifyFileExists(JTLFILESRC_TAG);

            string sourceLogFile = parameters.Value(JTLFILESRC_TAG);
            outputStream = new StreamWriter(parameters.Value(DESTFILE_TAG));

            // Check input format
            Log.WriteLine("check input format...");
            CheckInputFormat(sourceLogFile);

            // Compress and write to output
            Log.WriteLine("compressing...");
            DoCompress(sourceLogFile);

            double compression = (srcLinesCount / destLinesCount) * 100;

            Log.WriteLine("source=" + srcLinesCount + " compressed=" + destLinesCount + "(compression=" + Math.Round(compression, 0) + "%)");
        }

        /// <summary>
        /// Perform Jmeter log compression
        /// </summary>
        /// <param name="sourceLogFile"></param>
        /// <param name="destinationLogFile"></param>
        private void DoCompress(string sourceLogFilename)
        {
            StreamReader sr = new StreamReader(sourceLogFilename);
                     
            string line;

            while (!sr.EndOfStream)
            {
                line = sr.ReadLine();
                srcLinesCount++;

                if (IsUsefulLine(line))
                {
                    if (IsKnownOneliner(line))
                    {
                        if (!IsUselessOneliner(line))
                            ConsumeOnelineXMLTag(line);
                    }
                    else
                    {
                        if (line.Contains(JMXMON_TAG))
                            ConsumeMultilineXMLTag(sr, JMXMON_TAG);

                        // add more known multiline tags to include
                    }
                }
            }

            FlushOutput();
        }



        /// <summary>
        /// If line needs attention
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private bool IsUsefulLine(string line)
        {
            return (line.Trim().Length > 0);
        }

        /// <summary>
        /// Some oneliners are valid but useless (redirects, css, js...)
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private bool IsUselessOneliner(string line)
        {
            // generated subtransactions are useless
            return line.Contains("lb=\"http");
        }

        private void ConsumeMultilineXMLTag(StreamReader sr, string xmltag)
        {
            Regex reLabel = new Regex("<label>(.*)</label>");
            Regex tsLabel = new Regex("<ts>(.*)</ts>");
            Regex valueLabel = new Regex("<responseMessage>(.*)</responseMessage>");
            string lbPart = "";
            string tsPart = "";
            string valuePart = "";

            try
            {
                string line = sr.ReadLine();

                while ((!sr.EndOfStream) && (!line.Contains("/" + xmltag)))
                {
                    if ((reLabel.IsMatch(line)) && (!(reLabel.Match(line).Groups[1].Value == "true")))
                        lbPart += "lb=\"" + reLabel.Match(line).Groups[1].Value + "\"";

                    if (tsLabel.IsMatch(line))
                        tsPart += "ts=\"" + tsLabel.Match(line).Groups[1].Value + "\"";

                    if (valueLabel.IsMatch(line))
                        valuePart += "value=\"" + valueLabel.Match(line).Groups[1].Value + "\"";

                    line = sr.ReadLine();
                    srcLinesCount++;
                }

                string returnLine = "<" + xmltag + " " + tsPart + " " + lbPart + " " + valuePart + "/>";
                WriteToOutput(returnLine);
            }
            catch (Exception ex)
            {
                WriteToOutput("Error occured compressing multiline XML tag "+ xmltag);
                WriteToOutput(ex.Message);
            }

        }
                

        /// <summary>
        /// Correct and write this oneliner
        /// </summary>
        /// <param name="line"></param>
        private void ConsumeOnelineXMLTag(string line)
        {
            string outLine = CorrectXML(line);
            WriteToOutput( outLine.Trim() );
        }

        /// <summary>
        /// Write output
        /// </summary>
        /// <param name="outLine"></param>
        private void WriteToOutput(string outLine)
        {
            // temp to console, later to output file
            destLinesCount++;
            outputStream.WriteLine(outLine);
        }

        /// <summary>
        /// Flush rest of stream
        /// </summary>
        private void FlushOutput()
        {
            outputStream.Flush();
            outputStream.Close();
        }

        /// <summary>
        /// Correct XML of the line
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private string CorrectXML(string line)
        {
            if (!LeaveAloneConsumableLine(line) && !line.EndsWith("/>"))
                return line.Replace(">", "/>");
            else
                return line;
        }


        /// <summary>
        /// Lines that should be left alone (consume)
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private bool LeaveAloneConsumableLine(string line)
        {
            return (line.Contains("<?xml") || line.Contains("testResults"));
        }


        /// <summary>
        /// If no doubt this is one known oneliner
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private bool IsKnownOneliner(string line)
        {
            return (line.Contains("<?xml") || line.Contains("testResults") || line.Contains("<httpSample") || line.Contains("<sample"));
        }

       

        /// <summary>
        /// Basic check on input file validity
        /// </summary>
        /// <param name="sourceLogFile"></param>
        private void CheckInputFormat(string sourceLogFile)
        {
            if (!sourceLogFile.Contains(".jtl"))
                throw new FormatException("This is not a JTL file");
        }

    }
}
