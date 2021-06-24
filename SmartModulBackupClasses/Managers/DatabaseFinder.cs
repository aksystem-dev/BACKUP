using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartModulBackupClasses.Managers
{
    public class DatabaseFinder
    {
        public const string DBCACHE = "dbcache.txt";

        public class Database
        {
            public string name;
            public bool isNew;
        }

        public IEnumerable<Database> Get()
        {
            var config = Manager.Get<ConfigManager>().Config;

            List<string> dbNamesNew = new List<string>();
            using (var connection = new SqlConnection(config.Connection.GetConnectionString(1)))
            {
                connection.Open();

                using (var command = new SqlCommand("USE master; SELECT name FROM sys.databases", connection))
                using (var reader = command.ExecuteReader())
                {
                    while(reader.Read())
                    {
                        dbNamesNew.Add(reader.GetString(0));
                    }
                }
            }

            HashSet<string> dbNamesOld = new HashSet<string>();
            if (File.Exists(DBCACHE))
            {
                foreach (var line in File.ReadLines(DBCACHE))
                {
                    dbNamesOld.Add(line);
                }
            }

            File.WriteAllLines(DBCACHE, dbNamesNew);

            return dbNamesNew.Select(name => new Database()
            {
                name = name,
                isNew = !dbNamesOld.Contains(name)
            });
        }
    }
}
