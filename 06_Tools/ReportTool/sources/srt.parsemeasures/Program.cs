using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using srt.common;

namespace srt.parsemeasures
{
    class Program
    {
        // Read raw data and write it to intermediate file
        // this intermediate file content should be formatted as the target template needs thus include commandline separator characters
        // intermediate format should be search-and-replaceable in the target template, one merge tool for all types of intermediate values
        static void Main(string[] args)
        {
            Log.WriteLine("### srt.parsemeasures parser=[jmeter, silkperformer] <...> <intermediatefile>");
            Log.WriteLine("silkperformer: <transactionfilecsv>");
            Log.WriteLine("jmeter: <transacionfilejtl>");

            ParamInterpreter parameters = new ParamInterpreter();
            parameters.Initialize(args);
            parameters.ToConsole();

            MeasureController controller = null;

            if (parameters.Value("parser") == "jmeter")
            {
                controller = new JmeterMeasureController();
            }

            if (parameters.Value("parser") == "silkperformer")
            {
                controller = new SilkMeasureController();
            }

            if (controller == null)
                throw new Exception(string.Format("Not a valid parser [{0}]", parameters.Value("parser")));

            controller.Parse(parameters);

            Log.WriteLine("### srt.parsemeasures finished\n");
        }

    }
}
