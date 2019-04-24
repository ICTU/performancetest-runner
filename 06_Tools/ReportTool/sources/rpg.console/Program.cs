using System;
using rpg.common;

namespace rpg.console
{
    /// <summary>
    /// Commandline interface voor database manipulatie en vragen
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("### rpg.console - this is a console tool for basic teststraat data operations");
                Console.WriteLine(" [enabletestrun|disabletestrun] project=<projectname> testrun=<testrunpattern>");
                Console.WriteLine(" [createproject|deleteproject] project=<project>");
                Console.WriteLine(" listprojects");
                Console.WriteLine(" listtestruns project=<project>");
                Console.WriteLine(" deletetestrun project=<project> testrun=<testrun>");
                Console.WriteLine(" patterns are in all regex format, names are axact and not case sensitive");

                Environment.Exit(0);
            }

            ParamInterpreter _params = new ParamInterpreter();
            _params.Initialize(args, true);

            Globals.dbconnectstring = _params.Value("database");
            
            ConsoleController c = new ConsoleController();

            // Enable or disable test
            if (_params.Command == "enabletestrun") // core ready
            {
                c.EnableTestrun(_params.Value("project"), _params.Value("testrun"), true);
                return;
            }

            if (_params.Command == "disabletestrun") // core ready
            {
                c.EnableTestrun(_params.Value("project"), _params.Value("testrun"), false);
                return;
            }

            if (_params.Command == "listprojects") // core ready
            {
                c.ListProjectNamesToConsole();
                return;
            }

            if (_params.Command == "listtestruns") // core ready
            {
                c.ListTestrunNamesToConsole(_params.Value("project"));
                return;
            }

            if (_params.Command == "createproject") // core ready
            {
                c.CreateProject(_params.Value("project"));
                return;
            }

            if (_params.Command == "deleteproject")
            {
                c.DeleteProject(_params.Value("project"));
                return;
            }

            if (_params.Command == "deletetestrun")
            {
                c.DeleteTestrun(_params.Value("project"), _params.Value("testrun"));
                return;
            }

            Log.WriteLine(string.Format("Command [{0}] not found", _params.Command));
        }

    }
}
