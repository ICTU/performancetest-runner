using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Npgsql;
using System.Configuration;

namespace srt.common
{
    /// <summary>
    /// Static factory enabling reuse of one single connection throughout the session
    /// </summary>
    public static class Connection
    {
        private static string SERVERKEY = "dbserver";
        private static string PORTKEY = "dbport";
        private static string USERKEY = "dbusername";
        private static string PASSWORDKEY = "dbpassword";

        private static string _serverDefault = "localhost";
        private static string _portDefault = "5432";
        private static string _databaseDefault = "teststraat";
        private static string _usernameDefault = "postgres";
        private static string _passwordDefault = "postgres";

        private static NpgsqlConnection _instance = null;

        /// <summary>
        /// Connection (pool) instance
        /// </summary>
        public static NpgsqlConnection Instance
        {
            get { return GetConnection(); }
        }

        /// <summary>
        /// Create connection
        /// </summary>
        private static NpgsqlConnection GetConnection()
        {
            if (_instance == null)
            {
                string connectString = GetConnectString();
                _instance = new NpgsqlConnection(connectString);
                try
                {
                    Log.WriteLine(string.Format("connect to database {0}...", _databaseDefault));
                    _instance.Open();
                }
                catch (Exception e)
                {
                    Log.WriteLine(string.Format("FATAL: connect to database did not succeed [{0}]", connectString));
                    throw e;
                }
            }
            return _instance;
        }

        /// <summary>
        /// Build connect string
        /// </summary>
        /// <returns></returns>
        private static string GetConnectString()
        {
            var appSettings = ConfigurationManager.AppSettings;
            string server = appSettings[SERVERKEY] ?? _serverDefault;
            string port = appSettings[PORTKEY] ?? _portDefault;
            string username = appSettings[USERKEY] ?? _usernameDefault;
            string password = appSettings[PASSWORDKEY] ?? _passwordDefault;

            Log.WriteLine(string.Format("use database on {0}:{1}", server, port));

            // rewrite config with defaults if config is incomplete
            if ((appSettings[SERVERKEY] == null) || (appSettings[PORTKEY] == null) || (appSettings[USERKEY] == null) || (appSettings[PASSWORDKEY] == null))
            {
                try
                {
                    Configuration configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    KeyValueConfigurationCollection settings = configFile.AppSettings.Settings;

                    Log.WriteLine("config is incomplete, overwrite with default settings (please review)...");
                    Log.WriteLine(configFile.FilePath);

                    settings.Add(SERVERKEY, server);
                    settings.Add(PORTKEY, port);
                    settings.Add(USERKEY, username);
                    settings.Add(PASSWORDKEY, password);

                    configFile.Save(ConfigurationSaveMode.Modified);
                }
                catch { } // if no worky: jammer
            }
            string connectString = "Server=" + server + ";Port=" + port + ";User Id=" + username + ";Password=" + password + ";Database=" + _databaseDefault + ";Pooling=true";
            //Log.WriteLine("connect string ["+connectString+"]"); // debug info

            return connectString;
        }

        
    }

    /// <summary>
    /// Data access layer
    /// </summary>
    public class DataAccess
    {
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
            : base()
        {
            Initialize(project);
        }

        /// <summary>
        /// Raw object creation with connect, but without initialization
        /// </summary>
        public DataAccess()
            :base()
        {
            //Connect(_server, _port, _username, _password, _databasename);
            // do not initialize
        }

        /// <summary>
        /// Setup connection
        /// </summary>
        public void Initialize(string projectName)
        {
            //Connect(_server, _port, _username, _password, _databasename);
            RefreshRefProject(projectName);
            RefreshRefTestrun();
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
        public void InsertValue(string testrun, string category, string entity, string key, string value)
        {
            string testrunId = GetTestrunId(testrun, true);
            string queryString = string.Format("insert into VALUE (testrun_id, category, entity, key, value) values({0}, '{1}', '{2}', '{3}', '{4}')", testrunId, category, entity, key, value);
            ExecuteQuery(queryString);
        }

        /// <summary>
        /// Refresh reference hastable refProject
        /// </summary>
        private void RefreshRefProject(string projectName)
        {
            //Log.WriteLine("get project reference...");
            List<object> result = ExecuteQuery(string.Format("select id from PROJECT where name=upper('{0}')", projectName));
            try
            {
                List<string> row = (List<string>)result[0];
                _projectId = int.Parse(row[0]);
            }
            catch (Exception)
            {
                Log.WriteLine(string.Format("project [{0}] not found in database; add and get reference...", projectName));
                _projectId = AddProject(projectName);
            }
        }

        /// <summary>
        /// Add project
        /// </summary>
        /// <param name="projectName"></param>
        /// <returns>id</returns>
        private int AddProject(string projectName)
        {
            Log.WriteLine("adding project "+projectName+"...");
            string query = string.Format("insert into PROJECT (name) values ('{0}') returning id", projectName.ToUpper());
            try
            {
                List<object> result = ExecuteQuery(query);
                List<string> row = (List<string>)result[0];
                return int.Parse(row[0]);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("ERROR: adding project {0} failed, message: ", projectName, ex.Message));
            }
        }

        /// <summary>
        /// Get testrun ID or create if not exist (optional)
        ///  returns 0 if not exist and not created
        /// </summary>
        /// <param name="testrunName"></param>
        /// <param name="createIfNotExist"></param>
        /// <returns></returns>
        private string GetTestrunId(string testrunName, bool createIfNotExist)
        {
            //Log.WriteLine("get testrun id for {0}...", testrunName);
            if ((!_refTestrun.ContainsKey(testrunName)) && createIfNotExist)
            {
                Log.WriteLine(string.Format("testrun [{0}] not found, adding it...", testrunName));
                ExecuteQuery(string.Format("insert into TESTRUN (project_id, name) values ({0},'{1}') returning id", _projectId, testrunName));
                RefreshRefTestrun(); // new id is inserted in _refTestrun string array by this refresh
            }

            return GetSafeIdFromRef(_refTestrun, testrunName);
        }

        /// <summary>
        /// Get ID no matter if an entry is found (fallback to id=0)
        /// </summary>
        /// <param name="refTable"></param>
        /// <param name="entry"></param>
        /// <returns></returns>
        private string GetSafeIdFromRef(Hashtable refTable, string entry)
        {
            string idStr = "0";
            int id;

            try
            {
                idStr = (string)refTable[entry];
                if (!Int32.TryParse(idStr, out id))
                    idStr = "0";
            }
            catch
            {
                idStr = "0";
            }
                
            return idStr;
        }


        /// <summary>
        /// Reference table testrun (behorend bij huidig project)
        /// </summary>
        private void RefreshRefTestrun()
        {
            //Log.WriteLine("get testrun reference...");
            _refTestrun.Clear();
            List<object> result = ExecuteQuery(string.Format("select id, name from TESTRUN where project_id={0} and enabled=1 order by name", _projectId));

            foreach (List<string> row in result)
            {
                //Log.WriteLine("{0}-{1}",row[1], row[0]);
                _refTestrun.Add(row[1], row[0]); // name, id (key=name, value=id)
            }
        }

        private List<object> ExecuteQuery(string query)
        {
            //NpgsqlCommand command = new NpgsqlCommand(query, _connection);
            NpgsqlCommand command = new NpgsqlCommand(query, Connection.Instance);

            List<object> rows = new List<object>();

            try
            {
                NpgsqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    List<string> columns = new List<string>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        columns.Add(reader[i].ToString()); // alle columns in een row
                    }
                    rows.Add(columns); // alle rows
                }
            }
            catch (Exception ex)
            {
                // unique constraints door laten (wordt afgevangen op database), rest door laten naar boven
                if (ex.Message.Contains("unique"))
                {
                    Log.WriteLine("double row blocked by database");
                }
                else
                {
                    Log.WriteLine("I was executing query: "+query);
                    throw ex;
                }
            }
            return rows;
        }

        /// <summary>
        /// Get values belonging to testrun and key with wildcard
        /// </summary>
        /// <param name="testrunName"></param>
        /// <param name="category"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public List<object> GetValues(string testrunName, string category, string key)
        {
            string query = string.Format("select key, value from VALUE where testrun_id={0} and category like '{1}' and key like '{2}'",
                GetTestrunId(testrunName, false),
                TidyWildcards(category),
                TidyWildcards(key));
            //Log.WriteLine(query);
            return ExecuteQuery(query);
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
        /// Get Threshold values for current project
        /// </summary>
        public List<object> GetThresholds()
        {
            List<object> thresholds;

            string query = string.Format("select pattern, th1, th2 from THRESHOLD where project_id={0} order by pattern", _projectId);
            thresholds = ExecuteQuery(query);
            if (thresholds.Count == 0)
            {
                CreateGenericThresholds();
                thresholds = ExecuteQuery(query);
            }
            return thresholds;
        }

        /// <summary>
        /// Create default threshold set
        /// </summary>
        /// <returns></returns>
        private List<object> CreateGenericThresholds()
        {
            Log.WriteLine("create default threshold set...");
            string query = string.Format("insert into THRESHOLD (project_id, pattern, th1, th2) values ({0},'{1}',{2},{3})", _projectId, _defaultThresholdTag, _defaultThresholdTh1, _defaultThresholdTh2);
            return ExecuteQuery(query);
        }

        /// <summary>
        /// Get data (table, column) from a project table (with project_id)
        /// </summary>
        /// <param name="table"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        public List<object> GetProjectTableData(string table, string column)
        {
            string query = string.Format("select {0} from {1} where project_id={2} order by id", column, table, this._projectId);
            return ExecuteQuery(query);
        }

        /// <summary>
        /// Get TESTRUN names, includemode: 0=disabled 1=enabled 2=all
        /// </summary>
        /// <param name="project"></param>
        /// <param name="testrunNamePattern"></param>
        /// <param name="enabledFlag"></param>
        /// <returns></returns>
        private string[] GetTestrunNames(string project, string testrunNamePattern, int enabledFlag)
        {
            List<string> result = new List<string>();
            Regex runNameRegex = new Regex(testrunNamePattern);

            string query;

            switch (enabledFlag)
            {
                case 0:
                    query = string.Format("select name from TESTRUN where project_id={0} and enabled=0 order by name", _projectId);
                    break;
                case 1:
                    query = string.Format("select name from TESTRUN where project_id={0} and enabled=1 order by name", _projectId);
                    break;
                default:
                    query = string.Format("select name from TESTRUN where project_id={0} order by name", _projectId);
                    break;
            }

            List<object> testruns = ExecuteQuery(query); // volgorde bepaald door prim key! als order by name anders komt .18. voor .2.

            // alle testrun names: als voldoet aan regex pattern, dan opnemen in result
            foreach (List<string> row in testruns)
            {
                if (runNameRegex.IsMatch(row[0]))
                    result.Add(row[0]);
            }

            return result.ToArray();
        }

        /// <summary>
        ///  Get all testrun names matching regex pattern, only enabled testruns (default)
        /// </summary>
        /// <param name="project"></param>
        /// <param name="testrunNamePattern"></param>
        /// <returns></returns>
        public string[] GetTestrunNames(string project, string testrunNamePattern)
        {
            return GetTestrunNames(project, testrunNamePattern, 1);
        }

        /// <summary>
        /// Get all testrun names matching regex pattern, both enabled and disabled
        /// </summary>
        /// <param name="project"></param>
        /// <param name="testrunNamePattern"></param>
        /// <returns></returns>
        public string[] GetAllTestrunNames(string project, string testrunNamePattern)
        {
            return GetTestrunNames(project, testrunNamePattern, 2);
        }

        /// <summary>
        /// Get only disabled testrun names matching regex pattern
        /// </summary>
        /// <param name="project"></param>
        /// <param name="testrunNamePattern"></param>
        /// <returns></returns>
        public string[] GetDisabledTestrunNames(string project, string testrunNamePattern)
        {
            return GetTestrunNames(project, testrunNamePattern, 0);
        }

        /// <summary>
        /// Get all testrun names matching regex pattern, both enabled and disabled
        /// </summary>
        /// <param name="project"></param>
        /// <param name="testrunNamePattern"></param>
        /// <returns></returns>
        public string[] GetEnabledTestrunNames(string project, string testrunNamePattern)
        {
            return GetTestrunNames(project, testrunNamePattern, 1);
        }

        /// <summary>
        /// Get names of all projects
        /// </summary>
        /// <returns></returns>
        public string[] GetAllProjectNames()
        {
            List<string> projectNames = new List<string>();
            List<object> list = ExecuteQuery("select name from PROJECT order by name");

            foreach (List<string> projectName in list)
                projectNames.Add(projectName[0]);

            return projectNames.ToArray();
        }

        /// <summary>
        /// Get enabled testrun names where key=value in values table is as specified
        /// </summary>
        /// <param name="project"></param>
        /// <param name="testrunNamePattern"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public string[] GetTestrunNamesWithValue(string project, string testrunNamePattern, string key, string value)
        {
            List<string> result = new List<string>();
            Regex runNameRegex = new Regex(testrunNamePattern);

            string query = string.Format("select testrun.name from value, testrun where value.testrun_id=testrun.id and testrun.enabled=1 and value.key='{1}' and upper(value.value) like '{2}' and testrun.project_id={0} order by testrun.name", _projectId, key, value.ToUpper());
            List<object> testruns = ExecuteQuery(query); // volgorde bepaald door prim key! als order by name anders komt .18. voor .2.

            // alle testrun names: als voldoet aan regex pattern, dan opnemen in result
            foreach (List<string> row in testruns)
            {
                if (runNameRegex.IsMatch(row[0]))
                    result.Add(row[0]);
            }

            return result.ToArray();
        }

        /// <summary>
        /// Enable or disable one single testrun
        /// </summary>
        /// <param name="testname"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public void EnableTestrun(string testname, bool value)
        {
            string query = string.Format("update TESTRUN set enabled={0} where name='{1}'", value?1:0, testname);
            ExecuteQuery(query);
        }

        /// <summary>
        /// Create a new project
        /// </summary>
        /// <param name="projectName"></param>
        public void CreateProject(string projectName)
        {
            string query = string.Format("insert into PROJECT(name) values('{0}')", projectName);
            ExecuteQuery(query);
        }

        /// <summary>
        /// Delete (cascade) project
        /// </summary>
        /// <param name="projectName"></param>
        public void DeleteProject(string projectName)
        {
            //string query = string.Format("delete from PROJECT where name='{0}'", projectName);
            //ExecuteQuery(query);

            Log.WriteLine("delete values...");
            string query = string.Format("delete from VALUE where testrun_id in (select id from TESTRUN where project_id in (select id from PROJECT where name='{0}'))", projectName);
            ExecuteQuery(query);

            Log.WriteLine("delete testruns...");
            query = string.Format("delete from TESTRUN where project_id in (select id from PROJECT where name='{0}')", projectName);
            ExecuteQuery(query);

            Log.WriteLine("delete thresholds...");
            query = string.Format("delete from THRESHOLD where project_id in (select id from PROJECT where name='{0}')", projectName);
            ExecuteQuery(query);

            Log.WriteLine("delete project...");
            query = string.Format("delete from PROJECT where name='{0}'", projectName);
            ExecuteQuery(query);
        }

        /// <summary>
        /// Delete (cascade) testrun
        /// </summary>
        /// <param name="testrunName"></param>
        public void DeleteTestrun(string testrunName)
        {
            Log.WriteLine(string.Format("delete values related to testrun {0}...", testrunName));
            string query = string.Format("delete from VALUE where testrun_id in (select id from TESTRUN where name='{0}')", testrunName);
            ExecuteQuery(query);

            Log.WriteLine(string.Format("delete testrun related to testrun {0}...", testrunName));
            query = string.Format("delete from TESTRUN where name='{0}'", testrunName);
            ExecuteQuery(query);
        }
    }
}
