using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smart_modul_BACKUP_service
{
    /// <summary>
    /// Vytváří instance SqlBackuper s danými parametry.
    /// </summary>
    public class SqlBackuperFactory
    {
        public string ConnectionString;

        public SqlBackuperFactory(string connstr)
        {
            ConnectionString = connstr;
        }

        public SqlBackuper GetInstance()
        {
            return new SqlBackuper(ConnectionString);
        }
    }
}
