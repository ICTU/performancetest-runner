using System;
using rpg.common;

namespace rpg.merge
{
    class Program
    {
        public static string colCodeBelowTh1 = "green";
        public static string colCodeBetweenTh1Th2 = "yellow";
        public static string colCodeAboveTh2 = "red";
        public static string colCodeHighlite = "highlight";
        public static string colCodeBetterThanBaseline = colCodeBelowTh1;
        public static string colCodeWorseThanBaseline = colCodeAboveTh2;

        /// <summary>
        /// Merge van intermediate format (varnaam=value) csv file met ascii template met ${varnaam} of ${varnaam:index} variabele id's erin opgenomen
        /// let op, vanaf bron gaan we naar system-default systemlocale (.,:), pas bij merge formatteren we naar een geschikt output format(formatPattern)
        /// formatpattern voorbeelden: MM:ss yyyy 0000 0,000
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            Log.WriteLine("### rpg.merge [h (help), i (intermediate), v (variable), j (join), t (threshold), +t (with threshold), +d (with diff), +b (with baseline)] <templatefilename:name>");
            Log.WriteLine("### version " + typeof(Program).Assembly.GetName().Version.ToString());

            ParamInterpreter _params = new ParamInterpreter();
            _params.Initialize(args);
            _params.ToConsole();

            //LicenseManager lic = new LicenseManager();
            //Log.WriteLine(string.Format("License {0} (expiration {1})", lic.IsValid() ? "is valid" : "has expired", lic.ExpirationDateStr));

            if (_params.AskForHelp())
            {
                Log.WriteLine("Merge database- or commandline data with HTML template and generate output HTML");
                Log.WriteLine("merge i project=<project> testrun=<testrun> category=<category> entity=<entity>");
                Log.WriteLine("  merge values from intermediate (i) database values");
                Log.WriteLine("merge v project=<project> name=<name> value=<value>");
                Log.WriteLine("  merge variable (v) from commandline (name/value), alternatieve to <name> <value> for inserting counters: #");
                Log.WriteLine("merge t project=<project>");
                Log.WriteLine("  merge threshold (t) reference data");
                Log.WriteLine("merge it/vt");
                Log.WriteLine("  generate and merge threshold (t) evaluation from database intermediate (i) or variable (v) data");
                Log.WriteLine("merge id");
                Log.WriteLine("  generate and merge diff (d) evaluation (compware values), generate extra _c color code 'highlite'");
                Log.WriteLine("merge ib project=<project> testrun=<testrun> category=<category> entity=<entity> <baseline testrun reference tag>");
                Log.WriteLine("  generate and merge Baseline evaluation, generate extra tags _cb (baseline) _ce (evaluation: percentage diff)");
                Log.WriteLine("merge j/jt project=<project> testrun=<testrun> category=<category> entity=<entity> valueindex=<index> historycount=<count> workload=<workload>");
                Log.WriteLine("  join (j) series[index] values to new series from all testruns matching pattern testrun pattern (regex) with max of count items (0=nomax)");
                Log.WriteLine("  join and add threshold colorcodes (jt)...");
                Log.WriteLine("  template variable patterns:");
                Log.WriteLine("    ${varname:1:0.000} 1 = n th place value in ';' separated value list; 0.000 = number formatpattern [cnt, min, avg, max, p90, fail, cancel, p50, p99, stdev]");
                Log.WriteLine("    $[<td class=${varname_c:#}>${varname:#:0.000}</td>] = multiple replace pattern example");
                Log.WriteLine("general: beginpattern=<realpattern> endpattern=<realpattern> : only effective between begin- and end pattern");
            }

            // mandatory params
            string templateFilename = _params.Value("templatefile");

            // optional params - works only if both parameters are not *
            string beginPattern = _params.Value("beginpattern", "*");
            string endPattern = _params.Value("endpattern", "*");

            // check initial conditions before proceeding
            _params.VerifyMandatory("project");
            _params.VerifyMandatory("templatefile");
            _params.VerifyFileExists("templatefile");

            Globals.dbconnectstring = _params.Value("database");

            // create controller and initialialize for this project
            MergeController mergeController = new MergeController( _params.Value("project") );
            mergeController.ReadTemplate( templateFilename );

            if (_params.Function == 'i') // merge intermediate data
            {
                // read valueset for current testrun
                mergeController.ReadIntermediateValue(_params.Value("project"), _params.Value("testrun"), _params.Value("category"), _params.Value("entity", "*"));

                // Perform post-processing on data that was parsed and stored
                Log.WriteLine("Data post-processing phase...");

                // intermediate verrijken met evaluatie items
                if (_params.Switch == 't') mergeController.GenerateThresholdValues(colCodeBelowTh1, colCodeBetweenTh1Th2, colCodeAboveTh2); // threshold evaluatie -> kleurcodering (it= itermediate+threshold eval category x (trs))
                if (_params.Switch == 'd') mergeController.GenerateDiffValues(colCodeHighlite); // diff evaluatie -> kleurcodering (id = intermediate+diff eval category x (params))
                // auto keuze baseline komt alleen hier vandaan, dus baseline vars hier genereren
                if (_params.Switch == 'b') mergeController.GenerateBaselineValues(_params.Value("testrun"), _params.Value("baselinetestrun"), colCodeBetterThanBaseline, colCodeWorseThanBaseline);

                Log.WriteLine("Post-processing done, start merge...");

                // apply key prefix if configured
                if (_params.Value("prefix", "EMPTY") != "EMPTY")
                    mergeController.ApplyPrefix(_params.Value("prefix"));

                // merge result intermediate with template
                mergeController.MergeIntermediate(templateFilename, false, beginPattern, endPattern);
            }
            else if (_params.Function == 'v') // merge variable with external value
            {
                mergeController.MergeVariable(_params.Value("name"), _params.Value("value"), templateFilename, beginPattern, endPattern);
            }
            else if (_params.Function == 't') // merge thresholds
            {
                mergeController.ReadIntermediateThreshold(_params.Value("project"));

                // merge result intermediate with template
                mergeController.MergeIntermediate(templateFilename, true, beginPattern, endPattern);
            }
            else if (_params.Function == 'j') // join values historic overview
            {
                // join all indexed values into one intermediate
                mergeController.JoinIntermediateValues(_params.Value("project"), _params.Value("testrun", ".*"), _params.Value("category"), _params.Value("entity", "*"), _params.ValueInt("valueindex", "0"), _params.ValueInt("historycount", "10"), _params.Value("workload","*"));

                // evaluate thresholds if needed
                if (_params.Switch == 't') mergeController.GenerateThresholdValues(colCodeBelowTh1, colCodeBetweenTh1Th2, colCodeAboveTh2);

                // merge result intermediate with template
                mergeController.MergeIntermediate(templateFilename, true, beginPattern, endPattern);
            }
            else
            {
                throw new Exception("No parameters, nothing to do");
            }


            //mergeController.MergeLicenseInfo(templateFilename);

            Log.WriteLine("### rpg.merge finished\n");
        }
    }
}
