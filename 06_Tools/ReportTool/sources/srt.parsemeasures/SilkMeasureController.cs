using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections.Specialized;
using srt.common;

namespace srt.parsemeasures
{

    class SilkMeasureController: MeasureController
    {
        public const char SRCDECIMALSEPARATOR = ','; // Silkperformer measure source decimal separator

        const int NUMOFNAMEFIELDS = 4; // first x strings is name 
        const int NAMEDEFPOS = 1;
        const int VALUEPOS_AVG = 9; // position of AVG value
        const int VALUEPOS_SUM = 6; // position of SUM value
        const int NUMOFSERIESELEMENTS = 11;
        const int HEADERLINECOUNT = 6;

        const string CHAPTERSTARTSTRING = ";Summary General;---;Errors;";

        string[] csvLines;

        const string TRSCSVFILETAG = "transactionfilecsv";


        // Implemented by derived classes (standard returns false to force override)
        public override void CheckInputfileFormat(ParamInterpreter parameters)
        {
            string filename = parameters.Value(TRSCSVFILETAG);

            Log.WriteLine("checking file format (CSV + keyvalue) of " + filename);

            if (!Utils.IsCSVWithKey(filename, CHAPTERSTARTSTRING))
                throw new FormatException("file is not CSV format or missing crucial keywords: " + filename);
        }

        /// <summary>
        /// Silkperformer parser, toplevel executor
        /// </summary>
        /// <param name="p"></param>
        public override void DoParse(ParamInterpreter p)
        {
            // check if file exists
            p.VerifyFileExists(TRSCSVFILETAG);

            // read data
            csvLines = ReadLinesFromFile(p.Value(TRSCSVFILETAG));

            // extract names of measures
            Log.WriteLine("number of lines: "+csvLines.Length);
            _measureNames = ExtractMeasureNames(csvLines);

            // eerst definities (nodig voor config van grafieken) inlezen -> measuredetails
            ReadDefinitions(csvLines, new string[] {STARTTIMEKEY, INTERVALKEY});

            // dan measures (te plotten punten in de grafieken) inlezen -> measuredetails
            ReadMeasures(csvLines, _measureNames);
        }


        /// <summary>
        /// Extract measure names from source
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        private string[] ExtractMeasureNames(string[] lines)
        {
            Log.WriteLine("Extract measure names...");

            // read measure names first
            List<string> names = new List<string>();
            foreach (string line in lines)
            {
                if (IsSeriesLine(line))
                {
                    string name = NameFromLine(line);
                    if (!names.Contains(name) && name != "Name" && !name.EndsWith("type"))
                    {
                        names.Add(name);
                    }
                }
            }
            Log.WriteLine("number of measure names: " + names.Count);
            return names.ToArray();
        }

        /// <summary>
        /// Make all string before first number as a name
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private string NameFromLine(string line)
        {
            string name = "";
            string[] fields = line.Split(';');
            for (int i=0; i<NUMOFNAMEFIELDS; i++)
            {
                name = name + fields[i];
            }
            return name.Replace(' ','_');
        }

        /// <summary>
        /// If line meets basic requirements of measure line (number of elemens, 'avg' element is numeric)
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private bool IsSeriesLine(string line)
        {
            return (line.Split(';').Length == NUMOFSERIESELEMENTS) // num of values
                && (Utils.IsNumeric(line.Split(';')[VALUEPOS_AVG][0].ToString())); // value is numeric (not a header)
            // toevoegen: controle of trs voldoet aan naam pattern (\d\d_)
        }

        /// <summary>
        /// If line is first line of new chapter
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private bool IsNewChapter(string line)
        {
            return (line.StartsWith(CHAPTERSTARTSTRING));
        }

        /// <summary>
        /// Read definitions from measure source to in-memory measurelist
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="tags"></param>
        private void ReadDefinitions(string[] lines, string[] tags)
        {
            Log.WriteLine("Read definitions...");
            foreach (string tag in tags)
            {
                for (int i = 0; i < 6; i++)
                {
                    string line = lines[i];
                    string snippet = line.Split(';')[NAMEDEFPOS].TrimEnd(':');
                    if (snippet == tag)
                    {
                        _measureDetails.Add(tag, line.Split(';')[NAMEDEFPOS+1]);
                        Log.WriteLine(tag);
                    }
                }
            }
        }

        /// <summary>
        /// Read measure values from measure source tot in-memory measurelist
        /// - verwijderen als nieuwe succesvol blijkt
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="names"></param>
        private void ReadMeasuresOld(string[] lines, string[] names)
        {
            Log.WriteLine("Read meassure values... ");
            int valueCnt = 0;
            int stubCnt = 0;
            int valuePos = 0;
            int chapterCnt = 0;
            bool foundInChapter = false;

            // add values per name
            foreach (string name in names)
            {
                chapterCnt = 0;
                stubCnt = 0;
                valueCnt = 0;
                foundInChapter = false;

                //Log.WriteLine(name);
                foreach (string line in lines)
                {
                    if (IsSeriesLine(line))
                    {
                        if (NameFromLine(line) == name)
                        {
                            valuePos = name.Contains("[s]") ? VALUEPOS_AVG : VALUEPOS_SUM; // if seconds: avg; rest: sum
                            _measureDetails.Add(name, line.Split(';')[valuePos]);
                            valueCnt++;
                            foundInChapter = true;
                        }

                        // als nieuw (volgend) chapter: 0 schrijven voor deze measure voor vorig chapter als geen waarde gevonden
                        if (IsNewChapter(line))
                        {
                            //Log.WriteLine("chapterline found: "+line);
                            if ((!foundInChapter) && (chapterCnt!=0)) // add stub value if not measured in previous, skip first chapterline
                            {
                                _measureDetails.Add(name, "0.00000000");
                                stubCnt++;
                            }
                            chapterCnt++;
                        }
                    }
                }
                Log.WriteLine(string.Format("{0}: {1} values collected, {2} stubs added", name, valueCnt, stubCnt));
            }
        }

        /// <summary>
        /// Read measure values from measure source tot in-memory measurelist
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="names"></param>
        private void ReadMeasures(string[] lines, string[] names)
        {
            Log.WriteLine("Read meassure values... ");
            int valueCnt = 0;
            int stubCnt = 0;
            int valuePos = 0;
            string valueVal;
            bool foundInChapter = false;

            // bepaal voor alle namen apart de serie meetwaarden
            foreach (string name in names)
            {
                //chapterCnt = 0;
                stubCnt = 0;
                valueCnt = 0;
                foundInChapter = false;
                valueVal = "EMPTY";

                // ga lijn voor lijn na of er een meetwaarde in staat voor deze name/transactie en lees die in, let op: er zijn chapter/tijdvak overgangen
                foreach (string line in lines)
                {
                    // is het een meetwaarderegel?
                    if (IsSeriesLine(line))
                    {
                        try
                        {
                            // bevat deze regel een meetwaarde voor de name/transactie?
                            if (NameFromLine(line) == name)
                            {
                                valuePos = name.Contains("[s]") ? VALUEPOS_AVG : VALUEPOS_SUM; // if seconds: avg; rest: sum
                                valueVal = line.Split(';')[valuePos];
                                foundInChapter = true;
                            }

                            // bij chapterovergang bepalen of er een value gevonden is of 0-waarde moet worden toegevoegd
                            if (IsNewChapter(line))
                            {
                                if (foundInChapter) // add stub value if not measured in previous, skip first chapterline
                                {
                                    _measureDetails.Add(name, valueVal);
                                    valueCnt++;
                                }
                                else
                                {
                                    //measureDetails.Add(name, "0.00000000");
                                    _measureDetails.Add(name, "null"); // leeg/null is zuiverder dan 0, later in grafiek vervangen door placeholder
                                    stubCnt++;
                                }
                                foundInChapter = false;
                            }
                        }
                        catch (Exception e)
                        {
                            throw new Exception(String.Format("unexpected format of data found in line:\r\n [{0}]\r\n {1}", line, e.Message));
                        }
                    }
                }
                Log.WriteLine(string.Format("{0}: {1} values collected, {2} stubs added", name, valueCnt, stubCnt));
            }
        }

        /// <summary>
        /// Formatteren van measure data zodat deze straks direct in de template gezet kan worden
        /// format gebaseerd op HighCharts
        /// </summary>
        public override void FormatData()
        {
            Log.WriteLine("Format data...");

            // convert standard separators with custom ones
            _measureNames = _measureDetails.FormatMeasureData(_measureNames, SRCDECIMALSEPARATOR);

            // let op: dit is foutgevoelig, jmeter parser wordt met formatteren naar std formaat van DateTime bronwaarde, onderstaande is geknutsel

            // format start time value
            _measureDetails.items[STARTTIMEKEY] = _measureDetails.items[STARTTIMEKEY].Replace(":", MeasureDetails.MEASURETFIELDSEPARATOR.ToString());
            _measureDetails.items[STARTTIMEKEY] = _measureDetails.items[STARTTIMEKEY].Replace(".", MeasureDetails.MEASURETFIELDSEPARATOR.ToString());
            _measureDetails.items[STARTTIMEKEY] = _measureDetails.items[STARTTIMEKEY].Replace(" ", MeasureDetails.MEASURETFIELDSEPARATOR.ToString());
            _measureDetails.items[STARTTIMEKEY] = _measureDetails.items[STARTTIMEKEY].Replace(",0", MeasureDetails.MEASURETFIELDSEPARATOR.ToString());

            // format measure interval value
            double d = double.Parse(_measureDetails.items[INTERVALKEY]);
            _measureDetails.items[INTERVALKEY] = Math.Truncate(Math.Round(d*1000)).ToString();
         }

    }
}
