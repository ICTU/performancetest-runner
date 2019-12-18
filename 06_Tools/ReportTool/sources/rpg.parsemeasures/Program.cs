using System;
using rpg.common;

namespace rpg.parsemeasures
{
    class Program
    {
        // Read raw data and write it to intermediate file
        // this intermediate file content should be formatted as the target template needs thus include commandline separator characters
        // intermediate format should be search-and-replaceable in the target template, one merge tool for all types of intermediate values
        static void Main(string[] args)
        {
            Log.WriteLine("rpg.parsemeasures parser=[jmeter, silkperformer] <parameters (ask)>", true);
            Log.WriteLine("version " + typeof(Program).Assembly.GetName().Version.ToString());

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

            Log.WriteLine("rpg.parsemeasures finished\n", true);
        }

    }
}
