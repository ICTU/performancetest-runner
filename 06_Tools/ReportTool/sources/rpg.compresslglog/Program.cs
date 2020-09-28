using System;
using rpg.common;


namespace rpg.compresslglog
{
    class Program
    {
        static void Main(string[] args)
        {
            Log.WriteLine("rpg.compresslglog parser=[jmeter|silkperformer] <...> <out:destinationfile>", true);
            Log.WriteLine("version " + typeof(Program).Assembly.GetName().Version.ToString());

            ParamInterpreter parameters = new ParamInterpreter();
            parameters.Initialize(args);
            parameters.ToConsole();

            CompressController controller = null;

            if (parameters.Value("parser") == "jmeter")
            {
                controller = new JmeterCompressController();
            }

            if (parameters.Value("parser") == "silkperformer")
            {
                controller = new SilkCompressController();
            }

            if (controller == null)
                throw new Exception(string.Format("Not a valid parser [{0}]", parameters.Value("parser")));

            

            Log.WriteLine("start compression...");
            controller.Compress(parameters);

            Log.WriteLine("rpg.compresslglog finished\n", true);
        }
    }
}

