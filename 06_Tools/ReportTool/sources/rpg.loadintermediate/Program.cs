using rpg.common;

namespace rpg.loadintermediate
{
    class Program
    {
        //uitimo:
        //%toolspath%\srt.loadintermediate.exe %projcode% %testname% %temppath%\_transactions\(.*)\.(.*)\..*\.csv 
        //param1=projectname
        //param2=testname
        //param3=filename regex; group1=category; group2=entity

        //voor nu: %toolspath%\srt.loadintermediate.exe %projcode% %testname% msr perf %temppath%\_transactions.msr.perf.csv

        static void Main(string[] args)
        {

            Log.WriteLine("### rpg.loadintermediate <project> <testrun> <category> <entity> <intermediatefile> <database>");
            Log.WriteLine("### version " + typeof(Program).Assembly.GetName().Version.ToString());

            ParamInterpreter Params = new ParamInterpreter();
            Params.Initialize(args);
            Params.ToConsole();

            Params.VerifyFileExists("intermediatefile");

            Globals.dbconnectstring = Params.Value("database");

            LoadIntermediateController c = new LoadIntermediateController();

            // inlezen intermedate data (verificatie van validiteit)
            c.ReadIntermediate( Params.Value("intermediatefile") );

            // opslaan intermediate in database
            c.StoreIntermediate(Params.Value("project"), Params.Value("testrun"), Params.Value("category"), Params.Value("entity") );

            Log.WriteLine("### rpg.loadintermediate finished\n");

        }
    }
}
