using System;
using System.Collections.Generic;
using rpg.common;
using System.IO;
using System.Text.RegularExpressions;

namespace rpg.merge
{
    class MergeController
    {
        public Intermediate intermediate = new Intermediate();

        //public Intermediate intermediateAdded = new Intermediate();
        private Template template = new Template();
        private int counterValue = 0;
        private bool _regionEnabled = false;
        private string project;
        private string testrun;
        private string category;
        private string entity;
        private const string BASELINEREFVARNAME = "baselineref";
        private const string BASELINEREASONVARNAME = "baselinereason";
        private const string BASELINEWARNINGSKEY = "baselinewarnings";
        private const string THRESHOLDVIOLATIONSKEY = "thresholdviolations";
        private const string BASELINEKEYPREFIX = "baseline.";

        private const string REPEATPATTERN = @"\$\[(.*?)\]";
        
        public MergeController(string project) : base()
        {
            this.project = project;
        }

        /// <summary>
        /// Read intermediate data from VALUE table
        /// </summary>
        /// <param name="p_project"></param>
        /// <param name="p_testrun"></param>
        /// <param name="p_category"></param>
        /// <param name="p_entity"></param>
        public void ReadIntermediateValue(string p_project, string p_testrun, string p_category, string p_entity)
        {
            Log.WriteLine("read intermediate...");
            Log.WriteLine(string.Format("project/testrun={0}/{1}",project,testrun));

            // set intermediate reference values
            this.project = p_project;
            this.testrun = p_testrun;
            this.category = p_category;
            this.entity = p_entity;

            // read intermediate from VALUE table (common intermediate source)
            int cnt = intermediate.ReadFromDatabaseValue(this.project, this.testrun, this.category, this.entity);
            Log.WriteLine(cnt+" lines/variables");
        }

        /// <summary>
        /// Read table data into intermediate (any table)
        /// </summary>
        /// <param name="project"></param>
        public void ReadIntermediateThreshold(string project)
        {
            Log.WriteLine("read threshold intermediate...");
            Log.WriteLine(string.Format("project={0}", project));
            // read intermediate from <table>
            int cnt = intermediate.ReadFromDatabaseThreshold(project);
            Log.WriteLine(cnt + " lines/variables");
        }

        /// <summary>
        /// Read html template file with variable tags ${tag}
        /// </summary>
        /// <param name="filename"></param>
        public void ReadTemplate(string filename)
        {
            int cnt = 0;
            Log.WriteLine("read template...");
            Log.WriteLine(filename);
            using (StreamReader sr = new StreamReader(filename))
            {
                while (sr.Peek() >= 0)
                {
                    template.Add(sr.ReadLine());
                    cnt++;
                }
            }
            Log.WriteLine(cnt+" html template lines");
        }


        /// <summary>
        /// Merge and write output, expand line if used for joined data
        /// </summary>
        /// <param name="outputResultFilename"></param>
        /// <param name="expand"></param>
        /// <param name="beginPattern"></param>
        /// <param name="endPattern"></param>
        public void MergeIntermediate(string outputResultFilename, bool expand, string beginPattern, string endPattern)
        {
            Log.WriteLine(string.Format("merge intermediate and write output between [{0}] and [{1}]...", beginPattern, endPattern));
            using (StreamWriter sw = new StreamWriter(outputResultFilename))
            {
                foreach (string templateLine in template)
                {
                    MarkBetween(templateLine, beginPattern, endPattern);

                    string usedTemplateLine = expand ? ExpandTemplate(templateLine) : templateLine;
                    sw.WriteLine(Merge(usedTemplateLine, _regionEnabled)); // merge variables, but SKIP repeat parts
                }
            }
            Log.WriteLine("output to: "+outputResultFilename);
        }

        /// <summary>
        /// Merge a variable into a template
        /// </summary>
        /// <param name="varName"></param>
        /// <param name="varValue"></param>
        /// <param name="outputResultFilename"></param>
        /// <param name="beginPattern"></param>
        /// <param name="endPattern"></param>
        public void MergeVariable(string varName, string varValue, string outputResultFilename, string beginPattern, string endPattern)
        {
            Log.WriteLine(string.Format("merge variable and write output between [{0}] and [{1}]...", beginPattern, endPattern));
            using (StreamWriter sw = new StreamWriter(outputResultFilename))
            {
                foreach (string templateLine in template)
                {
                    MarkBetween(templateLine, beginPattern, endPattern);

                    if (varName == "*")
                        sw.WriteLine(StripVariables(templateLine, _regionEnabled));
                    else
                        sw.WriteLine(Merge(templateLine, varName, varValue, _regionEnabled));
                }
            }
            Log.WriteLine("output to: "+outputResultFilename);
        }

        /// <summary>
        /// Marks if templateLine is between beginPattern and endPattern (true) or not (false)
        ///  if between marks, returns true (do something)
        /// </summary>
        /// <param name="templateLine"></param>
        /// <param name="beginPattern"></param>
        /// <param name="endPattern"></param>
        /// <returns></returns>
        private void MarkBetween(string templateLine, string beginPattern, string endPattern)
        {
            // if one of the patterns is *: disable this function (isbetween=true)
            if ((beginPattern == "*") || (endPattern == "*"))
            {
                _regionEnabled = true;
            }
            else
            {
                // set mark on if beginPattern is detected and mark is not active
                if (templateLine.Contains(beginPattern))
                    _regionEnabled = true;
                // set mark off when endPattern is detected and mark is active
                if ((templateLine.Contains(endPattern)) && _regionEnabled)
                    _regionEnabled = false;
            }
        }

        /// <summary>
        /// Strip variable- and repeat placeholders form templateline
        /// </summary>
        /// <param name="templateLine"></param>
        /// <param name="enable"></param>
        /// <returns></returns>
        private string StripVariables(string templateLine, bool enable)
        {
            string resultLine = templateLine;
            if (!enable) return resultLine;

            Regex variablePattern = new Regex(@"\$\{.*?\}");
            Regex groupPattern = new Regex(@"\$\[.*?\]");

            // replace variable patterns
            foreach (Match match in variablePattern.Matches(resultLine))
            {
                resultLine = resultLine.Replace(match.Value, "");
                //Log.WriteLine(match.Value);
            }

            // replace group patterns
            foreach (Match match in groupPattern.Matches(resultLine))
            {
                resultLine = resultLine.Replace(match.Value, "");
                //Log.WriteLine(match.Value);
            }

            return resultLine;
        }

        /// <summary>
        /// Merge from intermediate file
        /// </summary>
        /// <param name="templateLine"></param>
        /// <param name="enable"></param>
        /// <returns></returns>
        private string Merge(string templateLine, bool enable)
        {
            return Merge(templateLine, string.Empty, string.Empty, enable);
        }

        /// <summary>
        /// The big merging trick (fill template variable with intermediate value)
        ///   skip lines with repeat patterns (first expand)
        /// </summary>
        /// <param name="templateLine"></param>
        /// <param name="varName"></param>
        /// <param name="varValue"></param>
        /// <param name="enable"></param>
        /// <returns></returns>
        private string Merge(string templateLine, string varName, string varValue, bool enable)
        {
            string resultLine = templateLine; // line blijft default ongewijzigd
            if (!enable) return resultLine;

            Regex varRegex = new Regex(@"\$\{(.*?)\}"); // zoek naar variabele tags volgens deze regex

            // als merge variabele pattern voor in een template regel, dan zoeken naar overeenkomstige variabele in template
            if ( varRegex.IsMatch(templateLine) )
            {
                string templateVariable;
                string templateVariableName;
                string templateValueIndex;
                string templateFormatPattern;
                string templateTotalPattern;
                string templateValueDefault;
                string returnValue;

                // itereren over alle variabelen in één template regel
                foreach (Match match in varRegex.Matches(templateLine))
                {
                    returnValue = "empty";
                    templateVariable = match.Groups[1].Value; // volledig pattern "blaat:0:0.00"; blaat:0:0.000:0 (laatste waarde is waarde als geen waarde gevonden, bijv: ""; "null"; "0" (string)

                    //templateVariableName = templateVariable.Contains(':') ? templateVariable.Split(':')[0] : templateVariable; // alleen variable name
                    //templateValueIndex = templateVariable.Contains(':') ? templateVariable.Split(':')[1] : "0"; // indexer
                    //templateFormatPattern = (templateVariable.Contains(':')) ? (templateVariable.Split(':').Length > 2) ? templateVariable.Split(':')[2] : string.Empty : string.Empty; // format pattern
                    //templateTotalPattern = match.Value; // compleet pattern "${blaat:0:0.000}"

                    bool isFormatPresent = templateVariable.Contains(':');
                    string[] fields = templateVariable.Split(':');
                    int fieldCnt = templateVariable.Split(':').Length;

                    templateVariableName = isFormatPresent ? fields[0] : templateVariable; // alleen variable name
                    templateValueIndex = isFormatPresent ? fields[1] : "0"; // indexer
                    templateFormatPattern = isFormatPresent ? (fields.Length > 2) ? fields[2] : string.Empty : string.Empty; // format pattern
                    templateValueDefault = isFormatPresent ? (fields.Length > 3) ? fields[3] : string.Empty : string.Empty;
                    templateTotalPattern = match.Value; // compleet pattern "${blaat:0:0.000:null}"

                    if (templateValueIndex != "#") // niet naar value zoeken als index nog open is (voor join extends)
                    {
                        // if general replace: handle here
                        if (varName == "*")
                        {
                            returnValue = varValue;
                        }
                        else
                        {
                            // if template variable matches parameter variable: return value; else: search intermediate
                            if (templateVariableName == varName)
                            {
                                if (templateVariableName == "#")
                                {
                                    returnValue = (++counterValue).ToString();
                                }
                                else
                                {
                                    returnValue = FormatValue(varValue, templateFormatPattern);
                                }
                            }
                            else
                            {
                                // stel result value samen (opzoeken juiste item in de lijst)
                                string val;
                                if (GetValueFromIntermediate(intermediate, templateVariable, out val)) // out val is leeg als GetValue hem niet gevonden heeft
                                {
                                    returnValue = FormatValue(val, templateFormatPattern);
                                }
                            }
                        }
                    }

                    // alleen iets doen als een waarde gevonden is, anders de template line intact laten en geen console log tonen
                    // ook niets doen als er nog repeat code in staat $[..], deze moet eerst expanded worden
                    if (returnValue != "empty")
                    {
                        string reason = "found";
                        bool isValueFound = (returnValue.Trim() != "");

                        // wanneer enkele waarde is werkvorm
                        if (!isValueFound && (templateValueDefault != string.Empty))
                        {
                            reason = "default";
                            returnValue = templateValueDefault;
                        }

                        Log.WriteLine(string.Format("value for variable replacement ({0}): {1}->[{2}] ", reason, templateVariable, returnValue));
                        resultLine = resultLine.Replace(templateTotalPattern, returnValue);
                    }
                } // foreach match in templateline
            } // if templateline contains match
            return resultLine;
        }

        /// <summary>
        /// Get the right value out of intermediate strings name=value1;value;value3
        /// searchpattern name name:0 name:1 ... (name=name:0)
        /// </summary>
        /// <param name="intermediate"></param>
        /// <param name="templateVariable"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool GetValueFromIntermediate(Intermediate intermediate, string templateVariable, out string value)
        {
            bool result = true;
            value = "";
            Regex regex = new Regex("^(.*?):([0-9]+)"); // variabelenaam:valueIndex
            try
            {
                // als een index aan de variabele toegevoegd is, dan value opzoeken in de comma separated value-rij, zo niet: index 0 gebruiken
                if (regex.IsMatch(templateVariable))
                {   
                    string varname = regex.Match(templateVariable).Groups[1].Value;
                    int valueIndex;
                    if (!int.TryParse(regex.Match(templateVariable).Groups[2].Value, out valueIndex))
                    {
                        // als geen indexer meegeggeven: neem dan index 0
                        valueIndex = 0;
                    }
                    value = intermediate.GetValue(varname, valueIndex);
                }
                else // als geen indexer aan variabele is toegevoegd: geef alleen intermediate terug als het een singleton is (geen list)
                {
                    if (!intermediate.GetValue(templateVariable).Contains(Intermediate.LISTSEPARATOR))
                    {
                        value = intermediate.GetValue(templateVariable);
                    }
                    else result = false;
                }
            }
            // als fout ontstaat is variabele niet gevonden in dit intermediate bestand, niet ernstig, value wordt dan niet aangeraakt!
            // het is aan aanroepend niveau om een defaultwaarde toe te kennen aan value
            catch { result = false; }

            return result;
        }

        /// <summary>
        /// Formatting of floatingpoint -> string values (string.Format pattern after {0}:
        /// 0.000 -> 5.876
        /// 0,000 -> 5,876
        /// </summary>
        /// <param name="value"></param>
        /// <param name="formatPattern"></param>
        /// <returns></returns>
        private string FormatValue(string value, string formatPattern)
        {
            string returnValue = value; // default
            string actualFormatPattern = formatPattern;

            // if floatseparator = ',', then temporarily change to '.' 
            bool isCommaAsFloatSep = formatPattern.Contains(",");
            if (isCommaAsFloatSep)
                actualFormatPattern = formatPattern.Replace(',','.');

            try
            {
                if (formatPattern != string.Empty)
                {
                    returnValue = float.Parse(value).ToString(actualFormatPattern);
                    returnValue = isCommaAsFloatSep ? returnValue.Replace('.', ',') : returnValue.Replace(',','.');
                }
            }
            catch { }

            return returnValue;
        }

        /// <summary>
        /// Expand repeat patterns $[..]
        /// </summary>
        /// <param name="templateLine"></param>
        /// <returns></returns>
        private string ExpandTemplate(string templateLine)
        {
            Regex regexRepeat = new Regex(REPEATPATTERN);
            string newTemplateLine = templateLine;

            try
            {
                // repeat for every expandpattern snippet found in this line
                foreach (Match match in regexRepeat.Matches(templateLine))
                {
                    string variable = ExtractVariableNames(match.Groups[1].Value)[0];
                    // only expand template pattern for variables found in the intermediate variablelist
                    if (intermediate.ContainsKey(variable))
                    {
                        string newPart = "";
                        for (int i = 0; i < intermediate.NumOfValues(variable); i++)
                        {
                            // duplicate pattern for each occurence of value, replacing # by value indexer
                            // patroon :# moet vervangen worden, niet # (kan deel van trsnaam zijn)
                            newPart = newPart + (match.Groups[1].Value).Replace(":#", ':' + i.ToString());
                        }
                        newTemplateLine = newTemplateLine.Replace(match.Groups[0].Value, newPart);
                        Log.WriteLine("expanding template for " + variable);
                    }
                }
            }
            catch
            {
                Log.WriteLine("WARNING expanding of variable repeat pattern failed (skipped) ["+templateLine+"]");
            }

            return newTemplateLine;
        }

        /// <summary>
        /// Extraction of variable names from line to string array
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private string[] ExtractVariableNames(string line)
        {
            Regex regex = new Regex(@"\$\{(.*?)\}");
            List<string> vars = new List<string>();
            foreach (Match match in regex.Matches(line))
            {
                vars.Add( match.Groups[1].Value.Split(':')[0] );
            }
            return vars.ToArray();
        }


        /// <summary>
        /// Generate new threshold colorcode values for all intermediate rows
        /// </summary>
        /// <param name="greenColorcode"></param>
        /// <param name="yellowColorcode"></param>
        /// <param name="redColorcode"></param>
        public void GenerateThresholdValues(string greenColorcode, string yellowColorcode, string redColorcode, bool storeMetrics=false)
        {
            Log.WriteLine("generate threshold values...");
            // load threshold config from database
            Thresholds thresholds = new Thresholds(project);

            // generate new color transactions
            Intermediate thValues = thresholds.GenerateThresholdValues(intermediate, greenColorcode, yellowColorcode, redColorcode);

            // merge threshold colortransactions with dataset
            Log.WriteLine(string.Format("adding {0} threshold entries...", thValues.Count));
            intermediate.Add(thValues);

            // count threshold violations (add in separate series)
            Log.WriteLine("aggregate threshold violations...");
            Intermediate thresholdViolations = thValues.AggregateCount(THRESHOLDVIOLATIONSKEY, @"\d\d_.*_c$", redColorcode); // only evaluate script transactions!

            if (storeMetrics)
                thresholdViolations.SaveToDatabase(this.project, this.testrun, Category.Transaction, Entity.None);

            Log.WriteLine("adding threshold violations: " + thresholdViolations.GetValue(THRESHOLDVIOLATIONSKEY));
            this.intermediate.Add(thresholdViolations);
        }

        /// <summary>
        /// Generate diff colorscheme for all loaded intermediate key/values
        /// compare values for all loaded key-value pairs (multiple value strings) and generate new _c color keys
        /// </summary>
        /// <param name="markColorcode"></param>
        public void GenerateDiffValues(string markColorcode)
        {
            Log.WriteLine("generate dif values...");
            Diff diff = new Diff();

            // generate new color transactions
            Intermediate diffValues = diff.GenerateDiffValues(intermediate, markColorcode);

            // merge threshold colortransactions with dataset
            Log.WriteLine(string.Format("adding {0} diff values", diffValues.Count));
            intermediate.Add(diffValues);
        }
        

        /// <summary>
        /// Perform baselining and generate extra baseline tags
        /// ${Appbeheer_01_Inloggen_br // reference (baseline value)
        /// ${Appbeheer_01_Inloggen_be // evaluated value (difference from baseline in %)
        /// ${Appbeheer_01_Inloggen_be_c // colorcode, mark when current value exceeds threshold th1, green when diff > -15%, red when diff > +15%
        /// </summary>
        /// <param name="currentTestrun"></param>
        /// <param name="baselineTestrun"></param>
        /// <param name="colorcodeBetter"></param>
        /// <param name="colorcodeWorse"></param>
        public void GenerateBaselineValues(string currentTestrun, string baselineTestrun, string colorcodeBetter, string colorcodeWorse, bool storeMetrics=false)
        {
            Thresholds thresholds = new Thresholds(this.project);

            // First, collect current and baseline run info and threshold violations:

            // lees baseline data, kies alternatief als baseline run niet gezet
            Log.WriteLine("read baseline values...");
            Intermediate baselineValues = ReadBaselineData(currentTestrun, baselineTestrun);

            Log.WriteLine("generate threshold evaluation on baseline run...");
            Intermediate baselineThresholdValues = thresholds.GenerateThresholdValues(baselineValues, Baselining.belowLowThreshold, "y", "r");
            // baseline: merge values with threshold colorcodes
            baselineThresholdValues.Add(baselineValues);

            Log.WriteLine("generate threshold evaluation on current run...");
            Intermediate currentThresholdValues = thresholds.GenerateThresholdValues(this.intermediate, Baselining.belowLowThreshold, "y", "r");
            // current testrun: merge values (this.intermediate) with threshold colorcodes
            currentThresholdValues.Add(this.intermediate);

            // Second, compare current run with baseline run:

            Log.WriteLine("generate baseline evaluation values...");
            Baselining baselining = new Baselining();
            Intermediate baselineEvaluation = baselining.GenerateBaselineEvaluationValues(currentThresholdValues, baselineThresholdValues, baselineValues, colorcodeBetter, colorcodeWorse);
            this.intermediate.Add(baselineEvaluation);
            Log.WriteLine("entries: " + baselineEvaluation.Count);

            // Third: generate aggregated metrics

            // Count baseline violations (add in separate series)
            Log.WriteLine("aggregate baseline warnings...");
            Intermediate baselineWarnings = baselineEvaluation.AggregateCount(BASELINEWARNINGSKEY, @"\d\d_.*_be_c", colorcodeWorse); // only evaluate script transactions!

            Log.WriteLine("adding baseline warnings: " + baselineWarnings.GetValue(BASELINEWARNINGSKEY));
            this.intermediate.Add(baselineWarnings);

            // store generated high-level stats
            if (storeMetrics)
            {
                // store baseline warning variables
                baselineWarnings.SaveToDatabase(this.project, this.testrun, Category.Transaction, Entity.None);

                // store calculated baseline reference chosen
                this.intermediate.SaveOneToDatabase(this.project, this.testrun, Category.Variable, Entity.Generic, BASELINEREFVARNAME);
            }
        }

        /// <summary>
        /// Read baseline data from database and return previous if baselineTestrunName does not exist
        /// </summary>
        /// <param name="currentTestrunName"></param>
        /// <param name="baselineTestrunName"></param>
        /// <returns></returns>
        private Intermediate ReadBaselineData(string currentTestrunName, string baselineTestrunName)
        {
            string baselineTestrunNameUsed = baselineTestrunName.Trim();
            Intermediate baselineReferenceValues = new Intermediate();
            bool baselineFound;
            string baselineReason = "specified";

            // lees baseline data
            Log.WriteLine("read baseline reference data...");
            baselineFound = (baselineReferenceValues.ReadFromDatabaseValue(this.project, baselineTestrunNameUsed, this.category, this.entity) > 0);

            // als baseline onbekend/niet gevuld: zoek naar geschikt alternatief
            if ((!baselineFound) || (baselineTestrunNameUsed.Length == 0))
            {
                Log.WriteLine(string.Format("baseline testrun [{0}] not found, search for suitable baseline...", baselineTestrunNameUsed));
                // dit is merge fase, huidige/nieuwe run zit al in de database, dus moeten we de vorige hebben
                DataAccess da = new DataAccess(this.project);

                string currentWorkload = da.GetValue(currentTestrunName, "workload");
                Log.WriteLine(string.Format("get testruns with same workload [{0}]...", currentWorkload));
                string[] testruns = da.GetTestrunNamesWithValue(this.project, ".*", "workload", currentWorkload);

                try
                {
                    // meest zuiver: neem de run vóór currentTestrun (vorige)
                    baselineTestrunNameUsed = testruns[Array.IndexOf(testruns, currentTestrunName) - 1];
                    baselineReason = "not specified, using previous"; 
                    Log.WriteLine(string.Format("automatic baselining: run before current run selected [{0}]", baselineTestrunNameUsed));
                }
                catch
                {
                    baselineTestrunNameUsed = currentTestrunName;
                    baselineReason = "not specified, using current";
                    Log.WriteLine("WARNING: no suitable baseline run found, use current run for comparison (no baselining)");
                }

                // 2e poging (fallback scenario)
                if (baselineReferenceValues.ReadFromDatabaseValue(this.project, baselineTestrunNameUsed, this.category, this.entity) == 0)
                    throw new Exception(string.Format("No baseline data found ({0}/{1}/{2})", this.project, this.category, this.entity));
            }

            // add baseline variables
            Log.WriteLine(string.Format("get variables from baseline [{0}]...", baselineTestrunNameUsed));
            if (intermediate.ReadFromDatabaseValue(this.project, baselineTestrunNameUsed, Category.Variable, "*", BASELINEKEYPREFIX) == 0)
                Log.WriteLine("WARNING: no variable data found for baseline");

            // add chosen baseline reference to intermediate (to be merged lateron) with reason
            AddInternalToIntermediate(BASELINEREFVARNAME, baselineTestrunNameUsed);
            AddInternalToIntermediate(BASELINEREASONVARNAME, baselineReason);

            return baselineReferenceValues;
        }

        /// <summary>
        /// Add extra (internally generated) variable to intermediate
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        private void AddInternalToIntermediate(string name, string value)
        {
            Log.WriteLine(string.Format("add derived variable to intermediate [{0}]", name));
            intermediate.Add(name, value);
        }

        /// <summary>
        /// Join
        /// </summary>
        /// <param name="project"></param>
        /// <param name="testrun"></param>
        /// <param name="category"></param>
        /// <param name="entity"></param>
        /// <param name="valueIndex"></param>
        /// <param name="historyCount"></param>
        /// <param name="workload"></param>
        public void JoinIntermediateValues(string project, string testrun, string category, string entity, int valueIndex, int historyCount, string workload)
        {
            Log.WriteLine(string.Format("join intermediate values entity={0}...", entity));
            JoinController joinController = new JoinController();
            this.intermediate = joinController.JoinIntermediateValues(project, testrun, category, entity, valueIndex, historyCount, workload);
        }

        /// <summary>
        /// Insert external variable name/value into intermediate
        /// </summary>
        /// <param name="varName"></param>
        /// <param name="varValue"></param>
        public void ReadVariableValue(string varName, string varValue)
        {
            this.intermediate.AddValue(varName, varValue);
        }

        /// <summary>
        /// Apply prefix to intermediate key values
        /// </summary>
        /// <param name="prefix"></param>
        public void ApplyPrefix(string prefix)
        {
            Log.WriteLine("adding prefix ["+prefix+"] to all keys...");
            this.intermediate = IntermediateFactory.ApplyKeyPrefix(this.intermediate, prefix);
        }
    }
}
