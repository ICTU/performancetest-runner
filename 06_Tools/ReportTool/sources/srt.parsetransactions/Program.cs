using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using srt.common;

namespace srt.parsetransactions
{
    class Program
    {
        static void Main(string[] args)
        {
            Log.WriteLine("### srt.parsetransactions <in:parser [jmeter|silkperformer]> <parameters>");
            Log.WriteLine("parameters silkperformer: <in:transactionfilebrp> <in:transactionfilecsv> <out:intermediatefile>");
            Log.WriteLine("parameters jmeter: <in:transactionfilecsv> <out:intermediatefile>");
            
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

            Log.WriteLine("### srt.parsetransactions finished\n");
        }

    }
}
