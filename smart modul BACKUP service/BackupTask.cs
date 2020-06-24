using SmartModulBackupClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace smart_modul_BACKUP_service
{
    /// <summary>
    /// Obsahuje informaci o konkrétním spuštění pravidla: odkaz na pravidlo a čas, kdy se má spustit.
    /// Pro použití s BackupTimeline
    /// </summary>
    public class BackupTask
    {
        public BackupTaskState State { get; private set; } = BackupTaskState.NotStartedYet;
        public readonly DateTime ScheduledStart;
        public readonly BackupRule Rule;

        public BackupTask(BackupRule rule, DateTime scheduledStart)
        {
            Rule = rule;
            ScheduledStart = scheduledStart;
        }

        //událost, která se spustí poté, co je BackupTask hotov
        public event EventHandler Finished;

        //odkaz na objekt BackupInProgress, používaný pro předávání GUI info o probíhajících zálohách a obnovách
        private BackupInProgress progress;

        /// <summary>
        /// Spustí toto pravidlo na novém vlákně.
        /// </summary>
        public BackupInProgress Execute()
        {
            //BackupTasky jsou jednorázově použitelné; lze 
            if (State != BackupTaskState.NotStartedYet)
                throw new InvalidOperationException("Nelze spustip BackupTask, který již běží, nebo je již dokončil svou práci!");

            State = BackupTaskState.Running; //změna stavu
            progress = Utils.InProgress.NewBackup(); //vytvoření objektu pro komunikaci s GUI
            progress.TAG = this; //umožnit přístup k tomuto objektu skrze BackupInProgress
            new Thread(execution).Start(); //započít prácičku
            return progress; //vrátit vzniklý BackupInProgress, ať s ním nějak naloží
        }

        /// <summary>
        /// Samotný proces vyhodnocování, běží na samostatném vlákně.
        /// </summary>
        private void execution()
        {
            try
            {
                Manager.Get<Backuper>().ExecuteRuleSingleZip(Rule, true, progress);
                State = BackupTaskState.Finished;
            }
            catch (ThreadAbortException)
            {
                State = BackupTaskState.Aborted;
            }
            catch (Exception ex)
            {
                SMB_Log.LogEx(ex);
                State = BackupTaskState.Failed;
            }
            finally
            {
                Utils.InProgress.RemoveBackup(progress);
                Finished?.Invoke(this, null);
            }
        }
    }

    public enum BackupTaskState { NotStartedYet, Running, Finished, Aborted, Failed }
}
