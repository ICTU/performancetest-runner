using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using srt.common;


namespace srt.parsevariables
{
    class Program
    {
        static void Main(string[] args)
        {
            Log.WriteLine("### srt.parsevariables parser=[jmeter|silkperformer] <...> <out:intermediatefile>");
            Log.WriteLine("silkperformer: <transactionfilecsv> <transactionfilebrp>");
            Log.WriteLine("jmeter: <transactionfilejtl> <transactionfilecsv>");

            ParamInterpreter parameters = new ParamInterpreter();
            parameters.Initialize(args);
            parameters.ToConsole();

            VariableController controller = null;

            if (parameters.Value("parser") == "jmeter")
            {
                controller = new JmeterVariableController();
            }

            if (parameters.Value("parser") == "silkperformer")
            {
                controller = new SilkVariableController();
            }

            if (controller == null)
                throw new Exception(string.Format("Not a valid parser [{0}]", parameters.Value("parser")));

            controller.Parse(parameters);

            Log.WriteLine("### srt.parsevariables finished\n");
        }
    }
}
