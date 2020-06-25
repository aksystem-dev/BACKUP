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
    public class RuleScheduler
    {
        //public void ScheduleRules(TimeSpan forHowLong)
        //{
        //    var timeline = Manager.Get<BackupTimeline>();

        //    if (timeline.Running) timeline.Stop();
        //    timeline.Start(GetBackupTaskList(forHowLong));
        //}


        /// <summary>
        /// Naplánuje pravidla podle BackupRuleLoader a vrátí seznam BackupTasků
        /// </summary>
        /// <param name="forHowLong">Pravidla jsou plánována od DateTime.Now po DateTime.Now + forHowLong</param>
        public List<BackupTask> GetBackupTaskList(TimeSpan forHowLong)
        {
            int total = 0;

            DateTime start = DateTime.Now;
            DateTime end = start + forHowLong;

            Logger.Log($"Plánuji pravidla mezi {start} a {end}");

            List<BackupTask> backupTasks = new List<BackupTask>();
            var rules = Manager.Get<BackupRuleLoader>().Rules;
            foreach (var rule in rules)
            {
                if (!rule.Enabled)
                {
                    Logger.Log($"Pravidlo {rule.Name} zakázáno, kašlu na něj tedy");
                    continue;
                }

                Logger.Log($"Plánuji vyhodnocování {rule.Name}");

                var tasks = rule.GetBackupTasksInTimeSpan(start, end);

                //toto jenom vypisuje info do eventlogu, lze do dát pryč
                foreach (var t in tasks)
                    Logger.Log($"{rule.Name} se spustí v {t.ScheduledStart}, čili za {t.ScheduledStart - DateTime.Now}");

                //přidat to do listu
                backupTasks.AddRange(tasks);

                total += tasks.Count();
            }

            Logger.Log($"Naplánováno {total} spuštění pravidel.");

            return backupTasks;
        }
    }
}
