using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections.Specialized;
using srt.common;

namespace srt.parsemeasures
{

    class SilkController
    {
        const int NAMEPOS = 2;
        const int NAMEDEFPOS = 1;
        const int VALUEPOS = 9;
        const int MINLENGTHSERIES = 5;
        const int HEADERLINECOUNT = 6;
        const string STARTTIMETAG = "Time";
        const string INTERVALTAG = "Interval";

        string[] measureNames; // measure name tags

        private MeasureDetails measureDetails = new MeasureDetails();

        /// <summary>
        /// Read measures to in-memory intermediate format
        /// </summary>
        /// <param name="fileName"></param>
        public void ReadMeasures(string fileName)
        {
            Console.WriteLine("Read content...");
            Console.WriteLine(fileName);
            string[] lines = File.ReadAllLines(fileName);

            Console.WriteLine("number of lines: "+lines.Length);
            measureNames = ExtractMeasureNames(lines);

            // eerst definities (nodig voor config van grafieken) inlezen -> measuredetails
            ReadDefinitions(lines, new string[] {STARTTIMETAG, INTERVALTAG});

            // dan measures (te plotten punten in de grafieken) inlezen -> measuredetails
            ReadMeasures(lines, measureNames);
        }


        /// <summary>
        /// Extract measure names from source
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        private string[] ExtractMeasureNames(string[] lines)
        {
            Console.WriteLine("Extract meassure names...");

            // read measure names first
            List<string> names = new List<string>();
            foreach (string line in lines)
            {
                if (line.Split(';').Length > MINLENGTHSERIES)
                {
                    string name = line.Split(';')[NAMEPOS];
                    if (!names.Contains(name) && name != "Name")
                    {
                        names.Add(name);
                        Console.WriteLine(name);
                    }
                }
            }
            return names.ToArray();
        }

        /// <summary>
        /// Read definitions from measure source in memory (measurelist)
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="tags"></param>
        private void ReadDefinitions(string[] lines, string[] tags)
        {
            Console.WriteLine("Read definitions...");
            foreach (string tag in tags)
            {
                for (int i = 0; i < 6; i++)
                {
                    string line = lines[i];
                    string snippet = line.Split(';')[NAMEDEFPOS].TrimEnd(':');
                    if (snippet == tag)
                    {
                        string name = tag;
                        string value = line.Split(';')[NAMEDEFPOS + 1];
                        measureDetails.Add(name, value);
                        Console.WriteLine(name);
                    }
                }
            }
        }

        private void ReadMeasures(string[] lines, string[] names)
        {
            Console.WriteLine("Read meassures...");

            // add values per name
            foreach (string name in names)
            {
                Console.WriteLine(name);
                foreach (string line in lines)
                {
                    if (line.Split(';').Length > MINLENGTHSERIES)
                    {
                        if (line.Split(';')[NAMEPOS] == name)
                            measureDetails.Add(name, line.Split(';')[VALUEPOS]);
                    }
                }
            }
        }

        /// <summary>
        /// Definitions and measures -> 1 file
        /// </summary>
        /// <param name="filename"></param>
        public void WriteIntermediate(string filename)
        {
            Console.WriteLine("Write intermediate data...");
            Console.WriteLine(filename);

            measureDetails.WriteToFile(filename);
        }

        public void FormatData(char decSeparator, char valSeparator)
        {
            Console.WriteLine("Format data...");

            // convert standard separators with custom ones
            measureNames = measureDetails.FormatMeasureData(measureNames, decSeparator, valSeparator);

            // format start time value
            measureDetails.items[STARTTIMETAG] = measureDetails.items[STARTTIMETAG].Replace(":", ",");
            measureDetails.items[STARTTIMETAG] = measureDetails.items[STARTTIMETAG].Replace(".", ",");
            measureDetails.items[STARTTIMETAG] = measureDetails.items[STARTTIMETAG].Replace(" ", ",");
            measureDetails.items[STARTTIMETAG] = measureDetails.items[STARTTIMETAG].Replace(",0", ",");

            // format measure interval value
            double d = double.Parse(measureDetails.items[INTERVALTAG]);
            measureDetails.items[INTERVALTAG] = Math.Truncate(Math.Round(d*1000)).ToString();
         }
    }
}
