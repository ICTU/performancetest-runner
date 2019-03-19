using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using srt.common;

namespace srt.console
{
    class ConsoleController
    {
        /// <summary>
        /// Enable or disable test
        /// </summary>
        /// <param name="projectname"></param>
        /// <param name="testnamePattern"></param>
        /// <param name="value"></param>
        public void Enable(string projectname, string testnamePattern, bool value)
        {
            DataAccess da = new DataAccess(projectname);
            string[] testnames = da.GetAllTestrunNames(projectname, testnamePattern);

            if (testnames.Length == 0)
                Console.WriteLine(string.Format("warning: no testruns found for project {0} matching {1}", projectname, testnamePattern));

            foreach (string testname in testnames)
            {
                Console.WriteLine(string.Format("{0} testrun {1}", value?"enable":"disable", testname));
                da.EnableTestrun(testname, value);
            }
        }

        /// <summary>
        /// Spool testrun names to console
        /// </summary>
        /// <param name="projectName"></param>
        /// <param name="testrunPattern"></param>
        public void ListTestrunNamesToConsole(string projectName, string testrunPattern)
        {
            DataAccess da = new DataAccess(projectName);
            string[] enabledtestnames = da.GetTestrunNames(projectName, (testrunPattern=="")?".*":testrunPattern);
            string[] disabledtestnames = da.GetDisabledTestrunNames(projectName, (testrunPattern == "") ? ".*" : testrunPattern);

            if ((enabledtestnames.Length == 0) && (disabledtestnames.Length == 0))
                Console.WriteLine(string.Format("warning: no testruns found for project {0}", projectName));

            Console.WriteLine("Enabled:");
            foreach (string testname in enabledtestnames)
                Console.WriteLine(testname);
            Console.WriteLine("Disabled:");
            foreach (string testname in disabledtestnames)
                Console.WriteLine(testname);
        }

        public void CreateProject(string projectName)
        {
            DataAccess da = new DataAccess();
            da.CreateProject(projectName);
            Console.WriteLine(string.Format("project {0} created", projectName));
        }

        public void DeleteProject(string projectName)
        {
            DataAccess da = new DataAccess();
            da.DeleteProject(projectName);
            Console.WriteLine(string.Format("project {0} deleted", projectName));
        }

        public void ListProjectNamesToConsole()
        {
            DataAccess da = new DataAccess();
            string[] projectNames = da.GetAllProjectNames();
            foreach (string projectName in projectNames)
                Console.WriteLine(projectName);
        }

        public void DeleteTestrun(string projectName, string testrunPattern)
        {
            DataAccess da = new DataAccess(projectName);
            string[] testrunNames = da.GetAllTestrunNames(projectName, testrunPattern);
            if (testrunNames.Length > 1)
                throw new Exception(string.Format("More ({0}) testruns match the given testrun name pattern", testrunNames.Length));
            if (testrunNames.Length == 0)
                throw new Exception("No matching testrun found");
            da.DeleteTestrun(testrunNames[0]);
            Console.WriteLine(string.Format("Testrun {0} deleted", testrunNames[0]));
        }
    }
}
