using System.Collections.Generic;
using rpg.common;

namespace rpg.loadintermediate
{
    class LoadIntermediateController
    {
        Intermediate intermediate;

        /// <summary>
        /// Read intermediate file to memory structure
        /// </summary>
        /// <param name="filename"></param>
        public void ReadIntermediate(string filename)
        {
            Log.WriteLine("Read intermediate...");
            Log.WriteLine(filename);
            intermediate = new Intermediate();
            int count = intermediate.ReadFromFile(filename);
            Log.WriteLine(count.ToString()+" lines");
        }


        /// <summary>
        /// Write intermediate memory structure to database
        /// </summary>
        /// <param name="projectName"></param>
        /// <param name="testName"></param>
        /// <param name="category"></param>
        /// <param name="entity"></param>
        public void StoreIntermediate(string projectName, string testName, string category, string entity)
        {
            intermediate.SaveToDatabase(projectName, testName, category, entity);
        }
    }
}
