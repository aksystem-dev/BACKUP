using SmartModulBackupClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace smart_modul_BACKUP_service.BackupExe
{
    /// <summary>
    /// Obsahuje informaci o konkrétním spuštění pravidla: odkaz na pravidlo a čas, kdy se má spustit.
    /// Pro použití s BackupTimeline
    /// </summary>
    public partial class BackupTask
    {
        public static string TempDir;
        public static List<BackupTask> RunningBackupTasks { get; private set; } = new List<BackupTask>();

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
        public BackupInProgress Progress { get; private set; }

        public Task TheTask { get; private set; }

        /// <summary>
        /// Spustí toto pravidlo na novém vlákně.
        /// </summary>
        public BackupInProgress Execute()
        {
            //BackupTasky jsou jednorázově použitelné; lze 
            if (State != BackupTaskState.NotStartedYet)
                throw new InvalidOperationException("Nelze spustit BackupTask, který již běží, nebo je již dokončil svou práci!");

            State = BackupTaskState.Running; //změna stavu
            Progress = Utils.InProgress.NewBackup(); //vytvoření objektu pro komunikaci s GUI
            Progress.TAG = this; //umožnit přístup k tomuto objektu skrze BackupInProgress
            TheTask = Task.Run(backup).ContinueWith(result =>
            {
                State = result.Status == TaskStatus.Faulted ? BackupTaskState.Failed : BackupTaskState.Finished;
                lock (RunningBackupTasks)
                    RunningBackupTasks.Remove(this);
            });
            return Progress; //vrátit vzniklý BackupInProgress, ať s ním nějak naloží
        }

        /// <summary>
        /// Samotný proces vyhodnocování, běží na samostatném vlákně.
        /// </summary>
        private void execution()
        {
            try
            {
                Manager.Get<Backuper>().ExecuteRuleSingleZip(Rule, true, Progress);
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
                Utils.InProgress.RemoveBackup(Progress);
                Finished?.Invoke(this, null);
            }
        }


        public bool IsCancelled { get; private set; }

        public void Cancel()
        {
            if (State == BackupTaskState.Running)
                IsCancelled = true;
            else
                throw new InvalidOperationException("Nelze zrušit zálohu, která neprobíhá");
        }

    }

    public enum BackupTaskState { NotStartedYet, Running, Finished, Aborted, Failed, Cancelled }
}
