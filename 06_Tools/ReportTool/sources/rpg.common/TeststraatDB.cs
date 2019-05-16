using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Microsoft.EntityFrameworkCore; // zit standaard in .NETCore

namespace rpg.common
{
    static class TeststraatDBFactory
    {
        private static TeststraatDB _instance = null;

        public static TeststraatDB GetTeststraatDB()
        {
            if (_instance == null)
            {
                _instance =  new TeststraatDB(Globals.dbconnectstring);
            }

            return _instance;
        }
    }

    public class TeststraatDB: DbContext
    {
        private string connectstring = ""; // is set with constructor

        public DbSet<project> project { get; set; }
        public DbSet<testrun> testrun { get; set; }
        public DbSet<value> value { get; set; }
        public DbSet<threshold> threshold { get; set; }

        /// <summary>
        /// Constructor, takes parameter hostname:database:user:password
        /// </summary>
        /// <param name="c"></param>
        public TeststraatDB(string c)
        {
            string[] parts = c.Split(":");

            if (parts.Length < 3)
                throw new Exception("DB connect string has less than 4 items");

            string phost = parts[0];
            string pusername = parts[parts.Length - 2];
            string ppassword = parts[parts.Length - 1];
            string pport = Utils.IsNumeric(parts[1]) ? parts[1] : "5432"; // port parameter is optional (default 5432)
            string pdatabase = "teststraat"; // database name is fixed

            connectstring = string.Format("Host={0};Port={1};Database={2};Username={3};Password={4}", phost, pport, pdatabase, pusername, ppassword);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseNpgsql(connectstring); // ontdekt zelf port (default?)
    }

    public class project
    {
        public int id { get; set; }
        public string name { get; set; }
    }

    public class testrun
    {
        public int id { get; set; }
        public string name { get; set; }
        public int enabled { get; set; }

        // foreign key to project
        public int project_id { get; set; }
    }

    public class value
    {
        public int id { get; set; }
        public string category { get; set; }
        public string entity { get; set; }
        public string key { get; set; }
        [Column("value")]
        public string _value { get; set; }

        // foreign key to testrun
        public int testrun_id { get; set; }
    }

    public class threshold
    {
        public int id { get; set; }
        public string pattern { get; set; }
        public float th1 { get; set; }
        public float th2 { get; set; }
        public int sort { get; set; }

        // foreign key to project
        public int project_id { get; set; }
    }

}
