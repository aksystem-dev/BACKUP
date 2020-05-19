using SmartModulBackupClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smart_modul_BACKUP_service
{
    public static class Extensions
    {
        public static TimeSpan Mod(this TimeSpan me, TimeSpan with)
        {
            while (me >= with)
                me -= with;
            while (me < TimeSpan.Zero)
                me += with;
            return me;
        }

        /// <summary>
        /// Vrátí všechny BackupTasky pro toto pravidlo od start po end.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public static IEnumerable<BackupTask> GetBackupTasksInTimeSpan(this BackupRule rule, DateTime start, DateTime end)
        {
            foreach (var i in rule.Conditions.AvailableDateTimesInTimeSpan(start, end, exclusiveStart: true))
            {
                var bt = new BackupTask()
                {
                    ScheduledStart = i,
                    Rule = rule
                };
                yield return bt;
            }
        }

        public static BackupTask GetBackupTaskRightNow(this BackupRule rule)
        {
            return new BackupTask()
            {
                ScheduledStart = DateTime.Now,
                Rule = rule
            };
        }
    }
}
