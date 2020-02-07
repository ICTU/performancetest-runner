using System.Collections.Generic;
using System.Linq;
using System.IO;
using rpg.common;
using System.Text.RegularExpressions;

namespace rpg.merge
{
    /// <summary>
    /// Join transaction values from different files to one (concat)
    /// </summary>
    class JoinController
    {
        public Intermediate intermediate = new Intermediate();

        /// <summary>
        /// Get source filenames
        /// </summary>
        /// <param name="containerDirectory"></param>
        /// <param name="subdirpattern"></param>
        /// <param name="intermediateFileIn"></param>
        /// <returns></returns>
        internal string[] GetSourceFileNames(string containerDirectory, string subdirpattern, string intermediateFileIn)
        {
            Log.WriteLine("getting source filenames...");   
            List<string> fileList = new List<string>();
            Regex regex = new Regex(subdirpattern);
            int totalCnt = 0;

            foreach (string dir in Directory.GetDirectories(containerDirectory) )
            {
                totalCnt++;
                if (regex.IsMatch(dir))
                {
                    string file = dir + @"\" + intermediateFileIn;
                    if (File.Exists( file ))
                    {
                        Log.Write(".");
                        fileList.Add( file );
                    }
                }
            }
            Log.WriteLine(totalCnt +" directories evaluated, "+ fileList.Count +" files found in directories matching pattern "+subdirpattern);
            return fileList.ToArray();
        }

        /// <summary>
        /// Get content from the intermediate file and join with the rest of the values
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="valueIndex"></param>
        internal void JoinIntermediateFromFile(string fileName, int valueIndex)
        {
            Log.WriteLine("joining intermediate "+fileName);

            Intermediate srcIntermediate = new Intermediate(); ;
            srcIntermediate.ReadFromFile(fileName);

            Intermediate indexedIntermediate = srcIntermediate.GetIndexedValues(valueIndex);

            this.intermediate.Expand(indexedIntermediate, Path.GetDirectoryName(fileName).Split('\\').Last());
        }

        /// <summary>
        /// Get content from the intermediate file and join with the rest of the values
        /// </summary>
        /// <param name="project"></param>
        /// <param name="testrun"></param>
        /// <param name="category"></param>
        /// <param name="entity"></param>
        /// <param name="valueIndex"></param>
        internal void JoinIntermediateFromDatabase(string project, string testrun, string category, string entity, int valueIndex)
        {
            Log.WriteLine(string.Format("joining intermediate {0}/{1}/{2}/{3}/{4}", project, testrun, category, entity, valueIndex));

            // read testrun intermediate from database
            Intermediate srcIntermediate = new Intermediate();
            int cnt = srcIntermediate.ReadFromDatabaseValues(project, testrun, category, entity);
            Log.WriteLine(string.Format("num of values read for entity [{0}/{1}]: {2}", category, entity, cnt));

            // filter only one indexed value per key
            Intermediate indexedIntermediate = srcIntermediate.GetIndexedValues(valueIndex);

            // add filtered list to result/target intermediate
            this.intermediate.Expand(indexedIntermediate, testrun);
        }

        /// <summary>
        /// Write intermediate file
        /// </summary>
        /// <param name="intermediateFileOut"></param>
        internal void WriteIntermediateToFile(string intermediateFileOut)
        {
            Log.WriteLine("writing intermediate...");
            Log.WriteLine(intermediateFileOut);
            this.intermediate.WriteToFile(intermediateFileOut);
        }

        /// <summary>
        /// Join values from database into result internal intermediate, work with testrun regex pattern
        /// </summary>
        /// <param name="project"></param>
        /// <param name="testrunPattern"></param>
        /// <param name="category"></param>
        /// <param name="entity"></param>
        /// <param name="valueIndex"></param>
        /// <param name="historyCount"></param>
        /// <param name="workload"></param>
        /// <returns></returns>
        public Intermediate JoinIntermediateValues(string project, string testrunPattern, string category, string entity, int valueIndex, int historyCount, string workload)
        {
            DataAccess da = new DataAccess(project);
            string[] testrunNames;

            Log.WriteLine(string.Format("get testruns for project [{0}] with testrun pattern [{1}] and workload [{2}]...", project, testrunPattern, workload));

            // collect all testruns (names) to join, with workload=xx (or leave workload if not there)
            if (workload == "")
                testrunNames = da.GetTestrunNames(project, testrunPattern);
            else
                testrunNames = da.GetTestrunNamesWithValue(project, testrunPattern, "workload", workload);

            // log status
            if (testrunNames.Length == 0)
                Log.WriteLine("WARNING: no testruns found!");
                //throw new Exception("No testrun found (including current) for building history"); // could be made blocking
            else
                Log.WriteLine(string.Format("{0} testruns found", testrunNames.Length));

            // add last x testruns to joined history set
            int endIdx = (testrunNames.Length > 0) ? testrunNames.Length - 1 : 0;
            int startIdx = ((endIdx - historyCount + 1) > -1) ? (endIdx - historyCount + 1) : 0;

            // show all history if historycount=0
            if (historyCount == 0)
                startIdx = 0;

            for (int i = startIdx; i <= endIdx; i++)
            {
                JoinIntermediateFromDatabase(project, testrunNames[i], category, entity, valueIndex);
            }

            return this.intermediate;
        }
    }
}
