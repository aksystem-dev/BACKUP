using SmartModulBackupClasses;
using SmartModulBackupClasses.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smart_modul_BACKUP_service
{
    /// <summary>
    /// Vytváří instance SqlBackuper s konfigurací podle aktuálního ConfigManageru.
    /// </summary>
    public class SqlBackuperFactory : IFactory<SqlBackuper>
    {
        public string ConnectionString;

        public SqlBackuper GetInstance()
        {
            return new SqlBackuper(Manager.Get<ConfigManager>().Config.Connection.GetConnectionString(10));
        }
    }
}
