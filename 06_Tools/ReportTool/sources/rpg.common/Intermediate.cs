using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace rpg.common
{
    /// <summary>
    /// Factory handling creation and initialization of intermediate objects
    /// </summary>
    public static class IntermediateFactory
    {
        /// <summary>
        /// Return intermediate where key=value are replaced with -prefix-key=value
        /// </summary>
        /// <param name="intermediate"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public static Intermediate ApplyKeyPrefix(Intermediate intermediate,string prefix)
        {
            Intermediate newIntermediate = new Intermediate();

            foreach (string key in intermediate.Keys)
            {
                newIntermediate.AddValue(prefix+key, intermediate[key]);
            }
            return newIntermediate;
        }
    }

    /// <summary>
    /// Class for handling intermediate data
    /// </summary>
    public class Intermediate : Dictionary<string, string>
    {
        public static string NOENTITY = "-";

        /// <summary> key-value pair separator </summary>
        public const char KEYVALUESEPARATOR = '=';
        /// <summary> list value separator character </summary>
        public const char LISTSEPARATOR = ';';
        /// <summary> commandline separator for column names </summary>
        public const char COLUMNSEPARATOR = ';'; // commandline separator for column names
        /// <summary> generic key name </summary>
        public string GenericThresholdKey = "generic";
        /// <summary> generic default thresholds </summary>
        public string GenericThresholdValue = "2" + Intermediate.LISTSEPARATOR + "4";
            
        /// <summary> tag key </summary>
        private const string TAGKEY = "tag";
        /// <summary> generic key prefix </summary>
        public string keyPrefix;

        private DataAccess dataAccess = null;

        /// <summary>
        /// Access method for dataAccess
        /// </summary>
        /// <param name="projectName"></param>
        /// <returns></returns>
        private DataAccess GetDataAccess(string projectName)
        {
            if (dataAccess == null)
                dataAccess = new DataAccess(projectName);

            return dataAccess;
        }

        /// <summary>
        /// Write intermediate to file
        /// </summary>
        /// <param name="keyPrefix"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
        public int WriteToFile(string keyPrefix, string filename)
        {
            //Log.WriteLine("Write to file "+filename);
            int cnt = 0;
            //string pf = (keyPrefix == "") ? "" : keyPrefix + "_";

            using (StreamWriter sw = new StreamWriter(filename))
            {
                foreach (KeyValuePair<string, string> item in this)
                {
                    sw.WriteLine(keyPrefix+item.Key.Trim() + KEYVALUESEPARATOR + item.Value.Trim());
                    cnt++;
                }
            }
            return cnt;
        }

        /// <summary>
        /// Overload
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public int WriteToFile(string filename)
        {
            return WriteToFile("", filename);
        }

        /// <summary>
        /// Read intermediate from file
        /// </summary>
        /// <param name="filename"></param>
        public int ReadFromFile(string filename)
        {
            int cnt = 0;
            using (StreamReader sr = new StreamReader(filename))
            {
                while (sr.Peek() > -1)
                {
                    string[] pair = sr.ReadLine().Split(KEYVALUESEPARATOR);
                    string value = String.Join(KEYVALUESEPARATOR.ToString(), pair, 1, pair.Count() - 1);
                    this.Add(pair[0], value);
                    cnt++;
                    //Log.WriteLine(pair[0]);
                }
            }
            return cnt;
        }

        /// <summary>
        /// Read intermediate data from database
        /// </summary>
        /// <param name="project"></param>
        /// <param name="testrun"></param>
        /// <param name="category"></param>
        /// <param name="key"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public int ReadFromDatabaseValue(string project, string testrun, string category, string key, string prefix)
        {
            DataAccess da = new DataAccess(project);
            //Log.Write(string.Format("readfromdatabasevalue {0}/{1}/{2}/{3}/{4} :", project, testrun, category, entity, prefix));
            int cnt = MergeDatabaseKeyValueResult( da.GetValues(testrun, category, key), prefix );
            //Log.WriteLine(cnt.ToString());
            da.DeInitialize();
            return cnt;
        }

        /// <summary>
        /// Overload without prefix
        /// </summary>
        /// <param name="project"></param>
        /// <param name="testrun"></param>
        /// <param name="category"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public int ReadFromDatabaseValue(string project, string testrun, string category, string entity)
        {
            return ReadFromDatabaseValue(project, testrun, category, entity, "");
        }
        
        /// <summary>
        /// Get intermediate with only one value from datasource series
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Intermediate GetIndexedValues(int index)
        {
            Intermediate filteredList = new Intermediate();
            foreach (KeyValuePair<string, string> pair in this)
            {
                string value = "";
                try
                {
                    string[] values = pair.Value.Split(Intermediate.LISTSEPARATOR);
                    value = values[index];
                }
                catch
                {
                    value = "";
                }
                filteredList.Add(pair.Key, value);
            }
            return filteredList;
        }

        /// <summary>
        /// Join new list with older values (concatenate with listseparator)
        /// </summary>
        /// <param name="intermediate"></param>
        /// <param name="tag"></param>
        public void Expand(Intermediate intermediate, string tag)
        {
            // Fix transaction entries and left length of value series
            Log.WriteLine(string.Format("expanding data for [{0}]", tag));

            // add key (transaction name) if not exist
            foreach (KeyValuePair<string, string> pair in intermediate)
            {
                if (!this.ContainsKey(pair.Key))
                    Add(pair.Key, "");
            }

            // normalize value length
            this.Normalize();

            // Add new values

            // add run tag
            AddValue(TAGKEY, tag);

            // add value
            foreach (KeyValuePair<string, string> pair in intermediate)
            {
                AddValue(pair.Key, pair.Value);
            }

            // normalize length of value series (to the right) for incomplete series
            this.Normalize();
        }

        /// <summary>
        /// Add value with listseparator
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void AddValue(string key, string value)
        {
            if (this.ContainsKey(key))
                this[key] = (this[key] == "") ? value : this[key] + LISTSEPARATOR + value;
                //this[key] += LISTSEPARATOR + value;
            else
                this.Add(key, value);
        }

        /// <summary>
        /// Fill values with blanks to normalize length of all values
        /// </summary>
        public void Normalize()
        {
            Log.WriteLine("normalize intermediate...");

            int cnt;
            int maxCnt = 0;

            // find max value series length
            foreach (KeyValuePair<string, string> pair in this)
            {
                cnt = NumOfElements(pair.Value);
                maxCnt = cnt > maxCnt ? cnt : maxCnt;
            }

            Intermediate tmpList = new Intermediate();

            // apply max value series length to shorter series (normalize)
            foreach (KeyValuePair<string, string> pair in this)
            {
                string newValue = pair.Value;

                // if new value: fill left
                if ((NumOfElements(newValue) == 1) && (maxCnt>1))
                {
                    //Log.Write("left fill:[" + pair.Key + "=" + pair.Value + "]");
                    while (NumOfElements(newValue) < maxCnt)
                        newValue = LISTSEPARATOR + newValue;
                    //Log.WriteLine("->[" + newValue + "]");
                }
                // if already present: fill right
                else
                {
                    //Log.Write("right fill:[" + pair.Key + "=" + pair.Value + "]");
                    while (NumOfElements(newValue) < maxCnt)
                        newValue = newValue + LISTSEPARATOR;
                    //Log.WriteLine("->[" + newValue + "]");
                }

                tmpList.Add(pair.Key, newValue);
            }
            this.ReplaceFrom(tmpList);
        }


        /// <summary>
        /// Copy content from dictionary
        /// </summary>
        /// <param name="workList"></param>
        public void ReplaceFrom(Intermediate workList)
        {
            this.Clear();
            foreach (KeyValuePair<string, string> pair in workList)
                this.Add(pair.Key, pair.Value);
        }

        

        /// <summary>
        /// Get number of element from value list
        /// </summary>
        /// <param name="elements"></param>
        /// <returns></returns>
        private int NumOfElements(string elements)
        {
            return elements.Split(LISTSEPARATOR).Length;
        }

        /// <summary>
        /// Number of values associated with this variable
        /// </summary>
        /// <param name="variableName"></param>
        /// <returns></returns>
        public int NumOfValues(string variableName)
        {
            if (this.ContainsKey(variableName))
                return NumOfElements(this[variableName]);
            else
                return 0;
        }

        /// <summary>
        /// Get the value list entry associated with index
        /// </summary>
        /// <param name="varname"></param>
        /// <param name="valueIndex"></param>
        /// <returns></returns>
        public string GetValue(string varname, int valueIndex)
        {
            return this[varname].Split(LISTSEPARATOR)[valueIndex];
        }

        /// <summary>
        /// Get the raw value field of this entry
        /// </summary>
        /// <param name="varname"></param>
        /// <returns></returns>
        public string GetValue(string varname)
        {
            return this[varname];
        }

        /// <summary>
        /// Get the values of this entry as string array
        /// </summary>
        /// <param name="varname"></param>
        /// <returns></returns>
        public string[] GetValueArray(string varname)
        {
            return GetValue(varname).Split(LISTSEPARATOR);
        }



        /// <summary>
        /// Add intermediate content of param to current/this value set
        /// </summary>
        /// <param name="intermediate"></param>
        public void Add(Intermediate intermediate)
        {
            foreach (KeyValuePair<string, string> pair in intermediate)
            {
                //Log.WriteLine("adding key=value {0}={1}", pair.Key, pair.Value);
                if (!this.ContainsKey(pair.Key))
                    this.Add(pair.Key, pair.Value);
            }
        }

        /// <summary>
        /// Insert data from database table into this (intermediate)
        /// </summary>
        /// <param name="project"></param>
        /// <returns></returns>
        public int ReadFromDatabaseThreshold(string project)
        {
            int cnt = 0;
            string columns = "pattern;th1;th2";

            DataAccess da = new DataAccess(project);

            foreach (string column in columns.Split(Intermediate.COLUMNSEPARATOR))
            {
                cnt += MergeDatabaseValueResult( da.GetThresholdData(column), "threshold."+column );
            }

            da.DeInitialize();

            return cnt;
        }

        /// <summary>
        /// Merge 1-column result into intermediate key-value pair with serialized values
        /// </summary>
        /// <param name="dbResult"></param>
        /// <param name="keyName"></param>
        /// <returns></returns>
        private int MergeDatabaseValueResult(List<object> dbResult, string keyName)
        {
            int cnt = 0;
            List<string> concatValue = new List<string>();

            if (dbResult.Count == 0)
                throw new Exception("dbResult is empty!");

            // insert into structure
            if (!this.ContainsKey(keyName))
            {
                // extract single value from each row and add to list
                foreach (string row in dbResult)
                {
                    //concatValue.Add(row[0]);
                    concatValue.Add(row);
                    cnt++;
                }
                // join list to one single ; separated string
                this.Add(keyName, string.Join(Intermediate.LISTSEPARATOR.ToString(), concatValue.ToArray() ));
            }
            else
                Log.WriteLine(string.Format("WARNING ignore intermediate duplicate [{0}]", keyName));

            return cnt;
        }

        /// <summary>
        /// Merge 2-column result into intermediate key-value pairs
        /// </summary>
        /// <param name="dbResult"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        private int MergeDatabaseKeyValueResult(List<object> dbResult, string prefix)
        {
            int cnt = 0;
            // insert into structure
            foreach (List<string> row in dbResult)
            {
                if (!this.ContainsKey(prefix+row[0]))
                {
                    this.Add(prefix+row[0], row[1]);
                    cnt++;
                }
                else
                    Log.WriteLine(string.Format("WARNING ignore intermediate duplicate [{0}]", prefix+row[0]));
            }
            return cnt; // num of rows matching rows
        }

        /// <summary>
        /// Return keys as string array
        /// </summary>
        /// <returns></returns>
        public string[] GetKeys()
        {
            List<string> keys = new List<string>();

            foreach (KeyValuePair<string,string> kvp in this)
                keys.Add(kvp.Key);

            return keys.ToArray();
        }

        /// <summary>
        /// Duplicate existing (current) currentKey=value to newKey=value
        /// </summary>
        /// <param name="existingKey"></param>
        /// <param name="newKey"></param>
        public void Duplicate(string existingKey, string newKey)
        {
            Log.WriteLine(string.Format("duplicate {0} to {1}...", existingKey, newKey));
            if (this.ContainsKey(existingKey) && !this.ContainsKey(newKey))
                this.Add(newKey, this[existingKey]);
        }

        /// <summary>
        /// Generate new intermedate with new counters holding aggregated values (per column)
        /// </summary>
        /// <param name="aggregateKey"></param>
        /// <param name="keyPattern"></param>
        /// <param name="valuePattern"></param>
        /// <returns></returns>
        public Intermediate AggregateCount(string aggregateKey, string keyPattern, string valuePattern)
        {
            Log.WriteLine(string.Format("calculate aggregate {0} on {1}={2} ...", aggregateKey, keyPattern, valuePattern));

            Regex regKey = new Regex(keyPattern);
            Regex regVal = new Regex(valuePattern);

            Intermediate result = new Intermediate();

            // add aggregate values
            for (int i = 0; i < this.GetMaxNumOfValues(); i++)
            {
                //Log.WriteLine("aggregate value position: " + i);
                int aggr = 0;
                foreach (string key in this.Keys)
                {
                    if (regKey.IsMatch(key))
                    {
                        try
                        {
                            // beveiligd try/catch ivm onzekerheid aantal entry's in de value reeks
                            if (regVal.IsMatch(GetValue(key, i)))
                            {
                                //Log.WriteLine(string.Format("key={0} value={1}", key, GetValue(key, i)));
                                aggr = aggr + 1;
                            }
                                
                        }
                        catch {}
                    }
                }
                //Log.WriteLine(string.Format("violations for position {0} = {1}", i, aggr.ToString()));
                result.AddValue(aggregateKey, aggr.ToString());
            }

            return result;
        }

        /// <summary>
        /// Get max number of values of all values stored in this matrix
        /// </summary>
        /// <returns></returns>
        private int GetMaxNumOfValues()
        {
            int cnt = 0;
            foreach (string key in Keys)
            {
                cnt = this.GetValueArray(key).Length > cnt ? this.GetValueArray(key).Length : cnt;
            }
            return cnt;
        }

        /// <summary>
        /// Save all content of this intermediate to database
        /// </summary>
        /// <param name="p_projectName"></param>
        /// <param name="p_testName"></param>
        /// <param name="p_category"></param>
        /// <param name="p_entity"></param>
        public void SaveToDatabase(string p_projectName, string p_testName, string p_category, string p_entity)
        {
            //DataAccess da = GetDataAccess(p_projectName);

            foreach (KeyValuePair<string, string> pair in this)
            {
                SaveOneToDatabase(p_projectName, p_testName, p_category, p_entity, pair.Key);
                //Log.WriteLine(string.Format("storing to database {0}|{1}|{2}|{3}|{4}...", p_projectName, p_testName, p_category, p_entity, pair.Key));
                //da.InsertValue(p_testName.Trim(), p_category.Trim(), p_entity.Trim(), pair.Key.Trim(), pair.Value.Trim());
            }
        }

        /// <summary>
        /// Save just one key/value pair in this intermediate to database
        /// </summary>
        /// <param name="p_projectName"></param>
        /// <param name="p_testName"></param>
        /// <param name="p_category"></param>
        /// <param name="p_entity"></param>
        /// <param name="p_key"></param>
        public void SaveOneToDatabase(string p_projectName, string p_testName, string p_category, string p_entity, string p_key)
        {
            DataAccess da = GetDataAccess(p_projectName);

            Log.WriteLine(string.Format("storing to database {0}|{1}|{2}|{3}|{4}...", p_projectName, p_testName, p_category, p_entity, p_key));
            da.InsertValue(p_testName.Trim(), p_category.Trim(), p_entity.Trim(), p_key.Trim(), this.GetValue(p_key).Trim());
        }

    }


}
