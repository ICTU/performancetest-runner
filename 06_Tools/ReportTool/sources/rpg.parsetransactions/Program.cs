using System;
using rpg.common;

namespace rpg.parsetransactions
{
    class Program
    {
        static void Main(string[] args)
        {
            Log.WriteLine("rpg.parsetransactions <in:parser [jmeter|silkperformer]> <parameters (ask)>", true);
            Log.WriteLine("version " + typeof(Program).Assembly.GetName().Version.ToString());

            ParamInterpreter parameters = new ParamInterpreter();

            parameters.Initialize(args);
            parameters.ToConsole();

            parameters.VerifyMandatory("parser");

            TransactionController controller = null;

            // read transaction data
            //ParseTransactionsSilkperformer(parameters);
            if (parameters.Value("parser")=="silkperformer")
            {
                Log.WriteLine("Creating Silkperformer parser...");
                controller = new SilkTransactionController();
            }

            //ParseTransactionsJMeter(parameters);
            if (parameters.Value("parser")=="jmeter")
            {
                Log.WriteLine("Creating JMeter parser...");
                controller = new JmeterTransactionController();
            }

            if (controller == null)
                throw new Exception( string.Format("Not a valid parser [{0}]", parameters.Value("parser")) );

            controller.Parse(parameters);

            Log.WriteLine("rpg.parsetransactions finished\n", true);
        }

    }
}
