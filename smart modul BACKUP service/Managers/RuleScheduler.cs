using smart_modul_BACKUP_service.BackupExe;
using SmartModulBackupClasses;
using SmartModulBackupClasses.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smart_modul_BACKUP_service.Managers
{
    /// <summary>
    /// Poskytuje metodu GetBackupTaskList
    /// </summary>
    public class RuleScheduler
    {
        //public void ScheduleRules(TimeSpan forHowLong)
        //{
        //    var timeline = Manager.Get<BackupTimeline>();

        //    if (timeline.Running) timeline.Stop();
        //    timeline.Start(GetBackupTaskList(forHowLong));
        //}

        private static void logError(string error, Exception ex = null)
            => SmbLog.Error(error, ex, LogCategory.RuleScheduler);

        private static void logInfo(string info, Exception ex = null)
            => SmbLog.Info(info, ex, LogCategory.RuleScheduler);

        /// <summary>
        /// Naplánuje pravidla podle BackupRuleLoader a vrátí seznam BackupTasků
        /// </summary>
        /// <param name="forHowLong">Pravidla jsou plánována od DateTime.Now po DateTime.Now + forHowLong</param>
        public List<BackupTask> GetBackupTaskList(DateTime start, DateTime end)
        {
            int total = 0;

            logInfo($"Plánuji pravidla mezi {start} a {end}");

            List<BackupTask> backupTasks = new List<BackupTask>();
            var rules = Manager.Get<BackupRuleLoader>().Rules;
            foreach (var rule in rules)
            {
                if (!rule.Enabled)
                {
                    logInfo($"Pravidlo {rule.Name} zakázáno, kašlu na něj tedy");
                    continue;
                }

                logInfo($"Plánuji vyhodnocování {rule.Name}");

                var tasks = rule.GetBackupTasksInTimeSpan(start, end);

                //toto jenom vypisuje info do eventlogu, lze do dát pryč
                foreach (var t in tasks)
                    logInfo($"{rule.Name} se spustí v {t.ScheduledStart}, čili za {t.ScheduledStart - DateTime.Now}");

                //přidat to do listu
                backupTasks.AddRange(tasks);

                total += tasks.Count();
            }

            logInfo($"Naplánováno {total} spuštění pravidel.");

            return backupTasks;
        }
    }
}
