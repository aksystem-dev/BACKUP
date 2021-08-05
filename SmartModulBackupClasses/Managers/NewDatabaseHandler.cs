using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartModulBackupClasses.Managers
{
    public class NewDatabaseHandler
    {
        public void CheckForNewDatabasesAndUpdateRulesAccordingly(IEnumerable<BackupRule> rules, Action<BackupRule> onDone, bool sendMails)
        {
            var newDatabases = Manager.Get<DatabaseFinder>().Get()
                .Where(db => db.isNew).Select(db => db.name);

            if (!newDatabases.Any()) { return; }

            if (sendMails)
            {
                SmbLog.Info("sending mail about new databases", category: LogCategory.Emails);
                StartSendingNewDatabasesEmail(newDatabases);
            }
            else
            {
                SmbLog.Info("not sending mail about new databases", category: LogCategory.Emails);
            }

            foreach (var rule in rules.Where(r => r.AutoBackupNewDatabases))
            {
                foreach (var databaseName in newDatabases.Where(dbName => !rule.Sources.Databases.Any(src => src.path == dbName)))
                {
                    SmbLog.Info($"automatically adding database '{databaseName}' to rule '{rule.Name}'");

                    rule.Sources.All.Add(new BackupSource()
                    {
                        enabled = true,
                        id = null,
                        path = databaseName,
                        type = BackupSourceType.Database
                    });
                }

                onDone?.Invoke(rule);
            }
        }

        private void StartSendingNewDatabasesEmail(IEnumerable<string> newDatabaseNames)
        {
            Task.Run(async () =>
            {
                try
                {
                    var mailer = Manager.Get<SmbMailer>();
                    await mailer.ReportNewDatabasesAsync(newDatabaseNames);
                }
                catch (Exception ex)
                {
                    SmbLog.Error("chyba v NewDatabaseHandleru při odesílání emailu", ex, LogCategory.Emails);
                }
            });
        }
    }
}
