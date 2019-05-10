using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace rpg.common
{
    /// <summary>
    /// Management of parameters
    /// </summary>
    public class ParamInterpreter
    {
        char CmdLineSplitChar = '=';

        /// <summary>Command</summary>
        public string Command;
        /// <summary>Function</summary>
        public char Function; // primary
        /// <summary>Switch</summary>
        public char Switch; // secondary

        private bool _interactive;
        private string[] _orgArguments;
        private Dictionary<string, string> _argumentList;

        /// <summary>
        /// Initialize object, read params into local structure
        /// </summary>
        /// <param name="arguments"></param>
        /// <param name="interactive"></param>
        public void Initialize(string[] arguments, bool interactive)
        {
            this._interactive = interactive;

            // if first argument is function(+switch), then extract them
            if (!arguments[0].Contains('='))
            {
                if (arguments[0].Length < 3) // function char with optional switch char
                {
                    Function = arguments[0][0];
                    if (arguments[0].Length > 1)
                        Switch = arguments[0][1];
                }
                else // function string
                {
                    Command = arguments[0];
                }
            }
            _orgArguments = arguments;
            _argumentList = DissectArgumentList(_orgArguments);
        }

        /// <summary>
        /// Initialize default non-interactive
        /// </summary>
        /// <param name="arguments"></param>
        public void Initialize(string[] arguments)
        {
            Initialize(arguments, false);
        }

        /// <summary>
        /// Initialize params and check number of arguments
        /// </summary>
        /// <param name="arguments"></param>
        /// <param name="numOfArguments"></param>
        public void Initialize(string[] arguments, int numOfArguments)
        {
            Initialize(arguments);
            if (_argumentList.Count != numOfArguments)
                throw new ArgumentException( string.Format("Wrong number of arguments, found {0} expected {1}", _argumentList.Count, numOfArguments) );
        }

        /// <summary>
        /// Get params from string param1:param1value param2:param2value
        /// </summary>
        /// <param name="_orgArguments"></param>
        /// <returns></returns>
        private Dictionary<string,string> DissectArgumentList(string[] _orgArguments)
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            string dummy;
            string key;

            //arguments are: "b=blaat"->key = b value = blaat
            foreach (string str in _orgArguments) // TODO could contain spaces! solve with doublequotes, workaround: delete pre- and post quotes before actual merge
            {
                int idx = str.IndexOf(CmdLineSplitChar);
                if (idx > -1)
                {
                    key = str.Split(CmdLineSplitChar)[0];
                    if (d.TryGetValue(key, out dummy))
                        throw new ArgumentException(string.Format("Double commandline argument: {0}", key));

                    string val = StripQuotation(str.Substring(idx + 1, str.Length - idx - 1));

                    d.Add(key, val);
                }
            }

            return d;
        }

        /// <summary>
        /// Strip string from any quotes
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private string StripQuotation(string value)
        {
            return value.Replace("\"","");
        }

        /// <summary>
        /// Get parameter value with default
        /// </summary>
        /// <param name="paramName"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public string Value(string paramName, string defaultValue)
        {
            try
            {
                return Value(paramName);
            }
            catch (Exception ex)
            {
                if (defaultValue != "")
                    return defaultValue;
                else
                    throw ex;
            }
        }

        /// <summary>
        /// Get parameter and convert to int
        /// </summary>
        /// <param name="paramName"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public int ValueInt(string paramName, string defaultValue)
        {
            try
            {
                return int.Parse( Value(paramName, defaultValue) );
            }
            catch (Exception)
            {
                throw new Exception("Parameter [" + paramName + "] is not an integer!");
            }
        }

        /// <summary>
        /// Get parameter value with optional value (return "" if empty)
        /// </summary>
        /// <param name="paramName"></param>
        /// <returns></returns>
        public string Value(string paramName)
        {
            string value = ""; // default return ""

            if (!_argumentList.TryGetValue(paramName, out value))
            {
                if (_interactive)
                {
                    value = AskForValue(paramName);
                    _argumentList.Add(paramName, value);
                }
                else
                    throw new Exception("Parameter " + paramName + " not available");
            }

            return value;
        }
        
        /// <summary>
        /// Return param value as Integer
        /// </summary>
        /// <param name="paramName"></param>
        /// <returns></returns>
        public int ValueInt(string paramName)
        {
            try
            {
                return int.Parse(Value(paramName));
            }
            catch (Exception)
            {
                throw new Exception("Parameter ["+paramName+"] is not an integer!");
            }
        }

        /// <summary>
        /// Ask for parameter value from command line
        /// </summary>
        /// <param name="paramName"></param>
        /// <returns></returns>
        private string AskForValue(string paramName)
        {
            Log.Write(paramName+"=");
            string value = Console.ReadLine();
            if (value == "")
                throw new Exception("Parameter " + paramName + " value empty");
            return value;
        }

        /// <summary>
        /// Is value equal to .. ?
        /// </summary>
        /// <param name="paramName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool ValueEq(string paramName, string value)
        {
            return (Value(paramName) == value);
        }

        /// <summary>
        /// Original argument string
        /// </summary>
        /// <returns></returns>
        public string ArgumentsStr()
        {
            return string.Join(" ", _orgArguments);
        }

        /// <summary>
        /// Number of arguments
        /// </summary>
        /// <returns></returns>
        public int NumOfArguments()
        {
            return _orgArguments.Length;
        }

        /// <summary>
        /// Make list of params with liinefeeds for console.writeline output
        /// </summary>
        public void ToConsole()
        {
            //Log.WriteLine("args: " + this.ArgumentsStr()); // prevent database login from being in log
            Log.WriteLine("number of argsuments: " + this.NumOfArguments());
            Log.WriteLine("function option = " + Function);
            Log.WriteLine("function switch = " + Switch);
            foreach (KeyValuePair<string, string> pair in _argumentList)
            {
                string val = (pair.Key == "database") ? "<hidden>" : pair.Value;
                Log.WriteLine(pair.Key + "=" + val);
            }
        }

        /// <summary>
        /// Verify if param is set, else throw exception
        /// </summary>
        /// <param name="p"></param>
        public void VerifyMandatory(string p)
        {
            if (!_argumentList.ContainsKey(p))
                throw new ArgumentException("Argument "+p+" not found");
        }

        /// <summary>
        /// Verify if param value file does exist
        /// </summary>
        /// <param name="p"></param>
        public void VerifyFileExists(string p)
        {
            if (!File.Exists( this.Value(p) ))
                throw new ArgumentException("File not found!", p);
        }

        /// <summary>
        /// Verify if help function is there
        /// </summary>
        /// <returns></returns>
        public bool AskForHelp()
        {
            return (_orgArguments[0] == "h");
        }

        /// <summary>
        /// Is param defined?
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool IsDefined(string name)
        {
            string val;
            return _argumentList.TryGetValue(name, out val);
        }
    }
}
