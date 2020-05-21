using SmartModulBackupClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smart_modul_BACKUP_service
{
    /// <summary>
    /// Obsahuje informaci o pravidlu a o tom, kdy se má spustit.
    /// Pro použití s BackupTimeline
    /// </summary>
    public class BackupTask
    {
        public bool Running { get; private set; } = false;
        public DateTime ScheduledStart;
        public BackupRule Rule;

        //událost, která se spustí poté, co je BackupTask hotov
        public event Action Finished;

        /// <summary>
        /// Spustí toto pravidlo na novém vlákně.
        /// </summary>
        public BackupInProgress Execute(Backuper backuper)
        {
            var progress = Utils.InProgress.NewBackup();
            progress.TAG = this;
            Task.Run(() =>
            {
                Running = true;
                backuper.ExecuteRuleSingleZip(Rule, true, progress);
                Running = false;
                Utils.InProgress.RemoveBackup(progress);
                Finished?.Invoke();
            });
            return progress;
        }
    }
}
