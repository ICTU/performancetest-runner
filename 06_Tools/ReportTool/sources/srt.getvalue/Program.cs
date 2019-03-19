using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using srt.common;

namespace srt_getvalue
{
    class Program
    {
        /// <summary>
        /// Ligth-weight value retrieval.
        /// input: Param1=csvFlename; param2=keyname
        /// output: value via stdout
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            Intermediate intermediate = new Intermediate();
            intermediate.ReadFromFile(args[0]);
            Console.Write(intermediate[args[1]]);
        }
    }
}
