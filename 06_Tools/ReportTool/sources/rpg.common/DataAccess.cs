using System;
using System.Collections.Generic;
using System.Collections;
using System.Text.RegularExpressions;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace rpg.common
{
    ///// <summary>
    ///// Static factory enabling reuse of one single connection throughout the session
    ///// </summary>
    //public static class Database
    //{
    //    private static TeststraatDB _instance = null;

    //    /// <summary>
    //    /// Create connection
    //    /// </summary>
    //    private static void Initialize()
    //    {
    //        _instance = new TeststraatDB();
    //        _instance.
    //        if (_instance == null)
    //        {
    //            string connectString = GetConnectString();
    //            _instance = new NpgsqlConnection(connectString);
    //            try
    //            {
    //                Log.WriteLine(string.Format("connect to database {0}...", _databaseDefault));
    //                _instance.Open();
    //            }
    //            catch (Exception e)
    //            {
    //                Log.WriteLine(string.Format("FATAL: connect to database did not succeed [{0}]", connectString));
    //                throw e;
    //            }
    //        }
    //        return _instance;
    //    }

    //    /// <summary>
    //    /// Build connect string
    //    /// </summary>
    //    /// <returns></returns>
    //    private static string GetConnectString()
    //    {
    //        string[] connectstrArray = Globals.dbconnectstring.Split(':');

    //        Log.WriteLine(string.Format("use database on {0}:{1}", connectstrArray[0], connectstrArray[1]));

    //        string connectString = "Server=" + connectstrArray[0] 
    //            + ";Port=" + connectstrArray[1] 
    //            + ";User Id=" + connectstrArray[2] 
    //            + ";Password=" + connectstrArray[3] 
    //            + ";Database=" + _databaseDefault 
    //            + ";Pooling=true";

    //        //Log.WriteLine("connect string ["+connectString+"]"); // debug info
    //        return connectString;
    //    }
    //}


    /// <summary>
    /// Data access layer
    /// </summary>
    public class DataAccess
    {
        public TeststraatDB _database = null;

        private int _projectId; // we werken altijd vanuit een project, entry in reftabel
        private Hashtable _refTestrun = new Hashtable(); // referentietabel testruns

        // vooralsnog statisch

        private const string _defaultThresholdTh1 = "1";
        private const string _defaultThresholdTh2 = "3";
        private const string _defaultThresholdTag = "generic";

        /// <summary>
        /// Object creation with initialization
        /// </summary>
        /// <param name="project"></param>
        public DataAccess(string project)
        {
            Initialize(project);
        }

        /// <summary>
        /// Object creation with initialization
        /// </summary>
        /// <param name="project"></param>
        public DataAccess()
        {
            Initialize();
        }

        /// <summary>
        /// Setup connection
        /// </summary>
        public void Initialize(string projectName = null)
        {
            _database = TeststraatDBFactory.GetTeststraatDB();

            if (projectName != null)
            {
                RefreshRefProject(projectName);
                RefreshRefTestrun();
            }
        }

        /// <summary>
        /// De initializer, destroy connectionpool
        /// </summary>
        public void DeInitialize()
        {
//            Disconnect();
        }

        private void Disconnect()
        {
//            if (_connection != null)
//                _connection.Close();

            //Log.WriteLine("database connection closed");
        }

        /// <summary>
        /// Insert key/value pair with all contextual info
        /// </summary>
        /// <param name="testrun"></param>
        /// <param name="category"></param>
        /// <param name="entity"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void InsertValue(string ptestrun, string pcategory, string pentity, string pkey, string pvalue)
        {
            int testrunId = GetTestrunId(ptestrun, true);

            value v = new value()
            {
                testrun_id = testrunId,
                category = pcategory,
                entity = pentity,
                key = pkey,
                _value = pvalue
            };

            try
            {
                _database.value.Add(v);
                _database.SaveChanges();
            }
            catch (Exception ex)
            {
                if (ex.InnerException.Message.Contains("unique constraint"))
                {
                    Log.WriteLine("EXCEPTION duplicate data, not inserted");
                    throw ex;
                }
                else
                {
                    throw ex;
                }
            }

            //string testrunId = GetTestrunId(testrun, true);
            //string queryString = string.Format("insert into VALUE (testrun_id, category, entity, key, value) values({0}, '{1}', '{2}', '{3}', '{4}')", testrunId, category, entity, key, value);
            //ExecuteQuery(queryString);
        }

        /// <summary>
        /// Refresh reference hastable refProject
        /// </summary>
        private void RefreshRefProject(string projectName)
        {
            List<project> projects = _database.project.Where(p => p.name.Contains(projectName.ToUpper())).OrderBy(p => p.id).ToList();

            if (projects.Count > 0)
                _projectId = projects.First().id;
            else
            {
                Log.WriteLine(string.Format("project [{0}] not found in database; add and get reference...", projectName));
                _projectId = AddProject(projectName);
            }

            ////Log.WriteLine("get project reference...");
            //List<object> result = ExecuteQuery(string.Format("select id from PROJECT where name=upper('{0}')", projectName));
            //try
            //{
            //    List<string> row = (List<string>)result[0];
            //    _projectId = int.Parse(row[0]);
            //}
            //catch (Exception)
            //{
            //    Log.WriteLine(string.Format("project [{0}] not found in database; add and get reference...", projectName));
            //    _projectId = AddProject(projectName);
            //}
        }

        //private void RefreshRefProject(string projectName)
        //{
        //    //Log.WriteLine("get project reference...");
        //    List<object> result = ExecuteQuery(string.Format("select id from PROJECT where name=upper('{0}')", projectName));
        //    try
        //    {
        //        List<string> row = (List<string>)result[0];
        //        _projectId = int.Parse(row[0]);
        //    }
        //    catch (Exception)
        //    {
        //        Log.WriteLine(string.Format("project [{0}] not found in database; add and get reference...", projectName));
        //        _projectId = AddProject(projectName);
        //    }
        //}

        /// <summary>
        /// Add project
        /// </summary>
        /// <param name="projectName"></param>
        /// <returns>id</returns>
        private int AddProject(string projectName)
        {
            Log.WriteLine("adding project "+projectName+"...");

            project newProject = new project();
            newProject.name = projectName.ToUpper();

            _database.project.Add(newProject);
            _database.SaveChanges();

            return newProject.id;

            //string query = string.Format("insert into PROJECT (name) values ('{0}') returning id", projectName.ToUpper());
            //try
            //{
            //    List<object> result = ExecuteQuery(query);
            //    List<string> row = (List<string>)result[0];
            //    return int.Parse(row[0]);
            //}
            //catch (Exception ex)
            //{
            //    throw new Exception(string.Format("ERROR: adding project {0} failed, message: ", projectName, ex.Message));
            //}
        }

        /// <summary>
        /// Get testrun ID or create if not exist (optional)
        ///  returns 0 if not exist and not created
        /// </summary>
        /// <param name="testrunName"></param>
        /// <param name="createIfNotExist"></param>
        /// <returns></returns>
        private int GetTestrunId(string testrunName, bool createIfNotExist)
        {
            if ((!_refTestrun.ContainsKey(testrunName)) && createIfNotExist)
            {
                Log.WriteLine(string.Format("testrun [{0}] not found, adding it...", testrunName));

                testrun tr = new testrun();
                tr.name = testrunName;
                tr.project_id = _projectId;
                tr.enabled = 1;

                var newTestrun = _database.testrun.Add(tr);
                _database.SaveChanges();

                RefreshRefTestrun(); // new id is inserted in _refTestrun string array by this refresh
            }

            string testrunId = GetSaveIdFromRefTestrun(testrunName);
            if (testrunId == "0")
                Log.WriteLine(string.Format("WARNING testrun [{0}] not found in table)", testrunId));

            return int.Parse(testrunId);


            ////Log.WriteLine("get testrun id for {0}...", testrunName);
            //if ((!_refTestrun.ContainsKey(testrunName)) && createIfNotExist)
            //{
            //    Log.WriteLine(string.Format("testrun [{0}] not found, adding it...", testrunName));
            //    ExecuteQuery(string.Format("insert into TESTRUN (project_id, name) values ({0},'{1}') returning id", _projectId, testrunName));
            //    RefreshRefTestrun(); // new id is inserted in _refTestrun string array by this refresh
            //}

            //return GetSafeIdFromRef(_refTestrun, testrunName);
        }

        /// <summary>
        /// Get ID no matter if an entry is found (fallback to id=0)
        /// </summary>
        /// <param name="refTable"></param>
        /// <param name="entry"></param>
        /// <returns></returns>
        private string GetSaveIdFromRefTestrun(string entry)
        {
            //Log.WriteLine(string.Format("getting testrun id for [{0}] from reference list...", entry));
            string idStr = "0";

            try
            {
                //Log.WriteLine("DEBUG search entry "+entry);
                //Log.WriteLine("DEBUG reflist count = " + _refTestrun.Count);
                idStr = (String)_refTestrun[entry];

                //Log.WriteLine("DEBUG idStr="+idStr);
                if (!Int32.TryParse(idStr, out int id))
                    idStr = "0";
            }
            catch
            {
                Log.WriteLine("warning: testrun not found in referencelist");
                idStr = "0";
            }
                
            return idStr;
        }


        /// <summary>
        /// Reference table testrun (behorend bij huidig project)
        /// </summary>
        private void RefreshRefTestrun()
        {

            Log.WriteLine("read testruns into reference list...");
            _refTestrun.Clear();

            var testruns = _database.testrun.Where(t => t.project_id == _projectId)
                .Where(t => t.enabled == 1)
                .OrderBy(t => t.name).ToList();

            //List<object> result = ExecuteQuery(string.Format("select id, name from TESTRUN where project_id={0} and enabled=1 order by name", _projectId));

            foreach (var t in testruns)
            {
                //Log.WriteLine("{0}-{1}",row[1], row[0]);
                _refTestrun.Add(t.name, t.id.ToString());
            }
        }

        //private List<object> ExecuteQuery(string query)
        //{
            ////NpgsqlCommand command = new NpgsqlCommand(query, _connection);
            //NpgsqlCommand command = new NpgsqlCommand(query, Database.Instance);

            //List<object> rows = new List<object>();

            //try
            //{
            //    NpgsqlDataReader reader = command.ExecuteReader();

            //    while (reader.Read())
            //    {
            //        List<string> columns = new List<string>();
            //        for (int i = 0; i < reader.FieldCount; i++)
            //        {
            //            columns.Add(reader[i].ToString()); // alle columns in een row
            //        }
            //        rows.Add(columns); // alle rows
            //    }
            //}
            //catch (Exception ex)
            //{
            //    // unique constraints door laten (wordt afgevangen op database), rest door laten naar boven
            //    if (ex.Message.Contains("unique"))
            //    {
            //        Log.WriteLine("double row blocked by database");
            //    }
            //    else
            //    {
            //        Log.WriteLine("I was executing query: "+query);
            //        throw ex;
            //    }
            //}
            //return rows;

            //return null;
        //}

        /// <summary>
        /// Get values belonging to testrun and key with wildcard
        /// </summary>
        /// <param name="testrunName"></param>
        /// <param name="category"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public List<object> GetValues(string testrunName, string category, string key)
        {
            var values = _database.value.Where(t => t.testrun_id == GetTestrunId(testrunName, false))
                .Where(t => EF.Functions.Like(t.category, TidyWildcards(category)))
                .Where(t => EF.Functions.Like(t.key, TidyWildcards(key))).ToList();

            List<object> result = new List<object>();

            foreach (value v in values)
            {
                List<string> row = new List<string>();
                row.Add(v.key);
                row.Add(v._value);

                result.Add(row);
            }

            //string query = string.Format("select key, value from VALUE where testrun_id={0} and category like '{1}' and key like '{2}'",
            //    GetTestrunId(testrunName, false),
            //    TidyWildcards(category),
            //    TidyWildcards(key));
            ////Log.WriteLine(query);
            //return ExecuteQuery(query);

            return result;
        }


        /// <summary>
        /// Get only first string result from database
        /// </summary>
        /// <param name="testrunName"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public string GetValue(string testrunName, string key)
        {
            List<string> row = (List<string>)GetValues(testrunName, "*", key)[0]; // first row
            return row[1]; // second value (key, value)
        }


        /// <summary>
        /// Transform wildcards (* -> %)
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private string TidyWildcards(string str)
        {
            return str.Replace('*', '%');
        }


        /// <summary>
        /// Get Threshold values for current project, order by sort, id
        /// </summary>
        public List<object> GetThresholds()
        {
            List<object> result = new List<object>();

            if (_database.threshold.Where(t => t.project_id == _projectId).Count() == 0)
            {
                CreateGenericThresholds();
            }

            var thresholds = _database.threshold.Where(t => t.project_id == _projectId).OrderBy(t => t.sort).ThenBy(t => t.id).ToList();

            foreach (threshold th in thresholds)
            {
                List<string> row = new List<string>();
                row.Add(th.pattern);
                row.Add(th.th1.ToString());
                row.Add(th.th2.ToString());

                result.Add(row);                
            }

            //List<object> thresholds;

            //string query = string.Format("select pattern, th1, th2 from THRESHOLD where project_id={0} order by pattern", _projectId);
            //thresholds = ExecuteQuery(query);
            //if (thresholds.Count == 0)
            //{
            //    CreateGenericThresholds();
            //    thresholds = ExecuteQuery(query);
            //}
            //return thresholds;

            return result;
        }

        /// <summary>
        /// Create default threshold set
        /// </summary>
        /// <returns></returns>
        private List<object> CreateGenericThresholds()
        {
            Log.WriteLine(string.Format("create default threshold {0}...", _defaultThresholdTag));

            threshold newThreshold = new threshold();
            newThreshold.project_id = _projectId;
            newThreshold.pattern = _defaultThresholdTag;
            newThreshold.th1 = float.Parse(_defaultThresholdTh1);
            newThreshold.th2 = float.Parse(_defaultThresholdTh2);

            _database.threshold.Add(newThreshold);
            _database.SaveChanges();

            //string query = string.Format("insert into THRESHOLD (project_id, pattern, th1, th2) values ({0},'{1}',{2},{3})", _projectId, _defaultThresholdTag, _defaultThresholdTh1, _defaultThresholdTh2);
            //return ExecuteQuery(query);
            return null;
        }

        /// <summary>
        /// Get data (table, column) from a project table (with project_id)
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        public List<object> GetThresholdData(string column)
        {
            var thresholds = _database.threshold.Where(t => t.project_id == _projectId).OrderBy(t => t.sort).ThenBy(t => t.id);

            List<object> returnValues = new List<object>();
            foreach (threshold th in thresholds)
            {
                if (column == "th1") returnValues.Add(th.th1.ToString());
                if (column == "th2") returnValues.Add(th.th2.ToString());
                if (column == "pattern") returnValues.Add(th.pattern);
            }
            //string query = string.Format("select {0} from {1} where project_id={2} order by id", column, table, this._projectId);
            //return ExecuteQuery(query);

            return returnValues;
        }

        /// <summary>
        /// Get TESTRUN names, includemode: 0=disabled 1=enabled 2=all {core}
        /// </summary>
        /// <param name="project"></param>
        /// <param name="testrunNamePattern"></param>
        /// <param name="enabledFlag"></param>
        /// <returns></returns>
        private string[] GetTestrunNames(string project, string testrunNamePattern, int enabledFlag = 2)
        {
            List<string> result = new List<string>();

            List<testrun> testruns;
            if (enabledFlag == 2)
                testruns = _database.testrun.Where(t => t.project_id == _projectId).OrderBy(t => t.name).ToList();
            else
                testruns = _database.testrun.Where(t => t.project_id == _projectId && t.enabled == enabledFlag).OrderBy(t => t.name).ToList();

            Regex runNameRegex = new Regex(testrunNamePattern);

            // alle testrun names: als voldoet aan regex pattern, dan opnemen in result
            foreach (var t in testruns)
            {
                if (runNameRegex.IsMatch(t.name))
                    result.Add(t.name);
            }

            return result.ToArray();
        }

        /// <summary>
        ///  Get all testrun names matching regex pattern, only enabled testruns (default) {core}
        /// </summary>
        /// <param name="project"></param>
        /// <param name="testrunNamePattern"></param>
        /// <returns></returns>
        public string[] GetTestrunNames(string project, string testrunNamePattern)
        {
            return GetTestrunNames(project, testrunNamePattern, 1);
        }

        /// <summary>
        /// Get all testrun names matching regex pattern, both enabled and disabled {core}
        /// </summary>
        /// <param name="project"></param>
        /// <param name="testrunNamePattern"></param>
        /// <returns></returns>
        public string[] GetAllTestrunNames(string project, string testrunNamePattern)
        {
            return GetTestrunNames(project, testrunNamePattern, 2);
        }

        /// <summary>
        /// Get only disabled testrun names matching regex pattern {core}
        /// </summary>
        /// <param name="project"></param>
        /// <param name="testrunNamePattern"></param>
        /// <returns></returns>
        public string[] GetDisabledTestrunNames(string project, string testrunNamePattern)
        {
            return GetTestrunNames(project, testrunNamePattern, 0);
        }

        /// <summary>
        /// Get all testrun names matching regex pattern, both enabled and disabled {core}
        /// </summary>
        /// <param name="project"></param>
        /// <param name="testrunNamePattern"></param>
        /// <returns></returns>
        public string[] GetEnabledTestrunNames(string project, string testrunNamePattern)
        {
            return GetTestrunNames(project, testrunNamePattern, 1);
        }

        /// <summary>
        /// Get names of all projects {core}
        /// </summary>
        /// <returns></returns>
        public string[] GetAllProjectNames()
        {
            List<string> names = new List<string>();

            var projects = _database.project.ToList();

            foreach (project p in projects)
            {
                names.Add(p.name);
            }

            return names.ToArray();    
        }

        /// <summary>
        /// Get enabled testrun names where key=value in values table is as specified
        /// </summary>
        /// <param name="project"></param>
        /// <param name="testrunNamePattern"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public string[] GetTestrunNamesWithValue(string pproject, string ptestrunNamePattern, string pkey, string pvalue)
        {
            List<string> result = new List<string>();
            Regex runNameRegex = new Regex(ptestrunNamePattern);

            // joinen in EF core snap ik niet, daarom values en testruns apart opgehaald
            // andersom (via testrun) doorlopen, dit lijkt het snelst te convergeren
            var testruns = _database.testrun
                .Where(t => t.project_id == _projectId)
                .Where(t => t.enabled == 1)
                .OrderBy(t => t.name).ToList();

            // zoeken in alle testruns bij dit project
            foreach(var _testrun in testruns)
            {
                // als testrunname aan zoekpatroon voldoet
                if (runNameRegex.IsMatch(_testrun.name))
                {
                    // dan kijken of er een value is die aan de voorwaarden voldoet
                    int v_cnt = _database.value
                        .Where(t => t.testrun_id == _testrun.id)
                        .Where(t => t.key == pkey)
                        .Where(t => EF.Functions.Like(t._value.ToUpper(), pvalue.ToUpper())).Count();

                    if (v_cnt > 0)
                    {
                        result.Add(_testrun.name);
                    }
                }
            }

            return result.ToArray();

            //string query = string.Format("select testrun.name from value, testrun where value.testrun_id=testrun.id and testrun.enabled=1 and value.key='{1}' and upper(value.value) like '{2}' and testrun.project_id={0} order by testrun.name", _projectId, key, value.ToUpper());
            //List<object> testruns = ExecuteQuery(query); // volgorde bepaald door prim key! als order by name anders komt .18. voor .2.

            //// alle testrun names: als voldoet aan regex pattern, dan opnemen in result
            //foreach (List<string> row in testruns)
            //{
            //    if (runNameRegex.IsMatch(row[0]))
            //        result.Add(row[0]);
            //}

            //return result.ToArray();
        }

        /// <summary>
        /// Enable or disable one single testrun
        /// </summary>
        /// <param name="testname"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public void EnableTestrun(string testname, bool value)
        {
            testrun tr = _database.testrun.Where(t => t.name == testname).First();
            tr.enabled = value ? 1 : 0;
            _database.SaveChanges();
        }

        /// <summary>
        /// Create a new project
        /// </summary>
        /// <param name="projectName"></param>
        public void CreateProject(string projectName)
        {
            project pr = new project();
            pr.name = projectName;
            _database.project.Add(pr);
            _database.SaveChanges();
        }

        /// <summary>
        /// Delete (cascade) project
        /// </summary>
        /// <param name="projectName"></param>
        public void DeleteProject(string projectName)
        {
            var aproject = _database.project.Where(t => t.name == projectName.ToUpper()).First();
            var testruns = _database.testrun.Where(t => t.project_id == aproject.id).ToList();
            var thresholds = _database.threshold.Where(t => t.project_id == aproject.id).ToList();

            Log.WriteLine(string.Format("there are {0} testruns associated with this project", testruns.Count));

            using (var transaction = _database.Database.BeginTransaction())
            {
                Log.WriteLine("delete values...");
                foreach (testrun t in testruns)
                {
                    var values = _database.value.Where(v => v.testrun_id == t.id).ToList();
                    _database.value.RemoveRange(values);
                }
                _database.SaveChanges();

                Log.WriteLine("delete testruns...");
                _database.testrun.RemoveRange(testruns);
                _database.SaveChanges();

                Log.WriteLine("delete thresholds...");
                _database.threshold.RemoveRange(thresholds);
                _database.SaveChanges();

                Log.WriteLine("delete project...");
                _database.project.Remove(aproject);
                _database.SaveChanges();

                transaction.Commit();
            }
        }

        /// <summary>
        /// Delete (cascade) testrun
        /// </summary>
        /// <param name="testrunName"></param>
        public void DeleteTestrun(string testrunName)
        {
            var atestrun = _database.testrun.Where(t => t.name == testrunName).First();
            var values = _database.value.Where(t => t.testrun_id == atestrun.id).ToList();

            using (var transaction = _database.Database.BeginTransaction())
            {
                Log.WriteLine("delete values...");
                _database.value.RemoveRange(values);
                _database.SaveChanges();

                Log.WriteLine("delete testrun...");
                _database.testrun.Remove(atestrun);
                _database.SaveChanges();

                transaction.Commit();
            }
        }
    }
}
