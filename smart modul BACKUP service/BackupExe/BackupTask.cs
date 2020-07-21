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
    /// Obsahuje informaci o konkrétním spuštění pravidla: odkaz na pravidlo a čas, kdy se má spustit (ten
    /// se používá v BackupTimeline). <br />
    /// Zároveň obsahuje kód pro vyhodnocení daného pravidla, který lze spustit zavoláním metody Execute().
    /// </summary>
    public partial class BackupTask
    {
        /// <summary>
        /// Pokaždé, když se spustí BackupTask, měl by se sám postarat, aby se přidal na tento seznam;
        /// poté, co je jeho vyhodnocování dokončeno, měl by se sám postarat, aby se z tohoto seznamu odebral
        /// </summary>
        private static List<BackupTask> _runningBackupTasks = new List<BackupTask>();

        /// <summary>
        /// Pole všech BackupTasků, které momentálně běží
        /// </summary>
        public static BackupTask[] RunningBackupTasks => _runningBackupTasks.ToArray();

        /// <summary>
        /// Vrátí, zdali aktuálně probíha BackupTask pro dané pravidlo
        /// </summary>
        /// <param name="rule"></param>
        /// <returns></returns>
        public static bool IsRuleExecuting(int ruleId)
        {
            return RunningBackupTasks.Any(task => task.Rule.LocalID == ruleId);
        }

        /// <summary>
        /// Stav, v němž se tento BackupTask nachází
        /// </summary>
        public TaskState State { get; private set; } = TaskState.NotStartedYet;

        /// <summary>
        /// Čas, kdy je tento BackupTask naplánovaný na spuštění; je to informace pro BackupTimeline
        /// </summary>
        public readonly DateTime ScheduledStart;

        /// <summary>
        /// Pravidlo, na nějž se tento BackupTask odkazuje
        /// </summary>
        public readonly BackupRule Rule;

        public BackupTask(BackupRule rule, DateTime scheduledStart)
        {
            Rule = rule;
            ScheduledStart = scheduledStart;
        }

        /// <summary>
        /// Událost, která se spustí poté, co je BackupTask hotov
        /// </summary>
        public event EventHandler Finished;

        /// <summary>
        /// odkaz na objekt BackupInProgress, používaný pro předávání GUI info o probíhajících zálohách a obnovách
        /// </summary>
        public BackupInProgress Progress { get; private set; }

        /// <summary>
        /// Objekt Task představující proces probíhající na pozadí
        /// </summary>
        public Task TheTask { get; private set; }

        /// <summary>
        /// Spustí toto pravidlo na novém vlákně.
        /// </summary>
        public BackupInProgress Execute()
        {
            //BackupTasky jsou jednorázově použitelné; lze 
            if (State != TaskState.NotStartedYet)
                throw new InvalidOperationException("Nelze spustit BackupTask, který již běží, nebo již dokončil svou práci!");

            State = TaskState.Running; //změna stavu

            lock (_runningBackupTasks)
                _runningBackupTasks.Add(this);

            Progress = Utils.InProgress.NewBackup(); //vytvoření objektu pro komunikaci s GUI
            Progress.RuleId = Rule.LocalID;
            Progress.RuleName = Rule.Name;
            Progress.TAG = this; //umožnit přístup k tomuto objektu skrze BackupInProgress
            Progress.AfterUpdateCalled += () => Utils.GUIS.UpdateBackup(Progress); //volání update na Progress automaticky informuje GUI
            Utils.GUIS.StartBackup(Progress);
                
            //metodu pro provedení zálohy rozhodneme podle typu pravidla
            Func<Task> backup_method;
            switch(Rule.RuleType)
            {
                case BackupRuleType.FullBackups:
                    //FullBackups => backupFull
                    backup_method = backupFull;
                    break;
                case BackupRuleType.OneToOne:
                    //OneToOne => backupOneToOne
                    backup_method = backupOneToOne;
                    break;
                case BackupRuleType.ProtectedFolder:
                    //ProtectedFolder => standartní backupFull, ale potom odstraníme data v
                    //chráněné složce; zde řešeno lambdou, páč kdo by kvůli jedné funkci navíc
                    //dělal celou novou metodu
                    backup_method = new Func<Task>(async () =>
                    {
                        await backupFull();

                        foreach (var source in Rule.Sources.Directories)
                            try
                            {
                                FileUtils.DeleteFolder(source.path, deleteSelf: false, log: true);
                            }
                            catch(Exception ex)
                            {
                                SmbLog.Error("skuhrot při odstraňování obsahu chráněné složky po její záloze; čím jsem si to zasloužil?", ex, LogCategory.BackupTask);
                            }
                    });
                    break;
                default:
                    throw new NotImplementedException($"Neznám typ zálohy {Rule.RuleType}");
            }

            TheTask = Task.Run(backup_method).ContinueWith(result =>
            {
                State = result.Status == TaskStatus.Faulted ? TaskState.Failed : TaskState.Finished;
                lock (_runningBackupTasks)
                    _runningBackupTasks.Remove(this);
                Utils.GUIS.CompleteBackup(Progress, B_Obj.LocalID);
                Manager.Get<ProgressManager>().RemoveBackup(Progress);
            });

            return Progress; //vrátit vzniklý BackupInProgress, ať s ním nějak naloží
        }

        /// <summary>
        /// Jestli byl tento BackupTask zrušen
        /// </summary>
        public bool IsCancelled { get; private set; }

        /// <summary>
        /// Zruší tento BackupTask; záloha se nedokončí, ale zruší se nějak chytře, aby po ní nezbyl bordel;
        /// zrušení se momentálně řeší tak, že vyhodnovací metoda v určitých místech kontroluje, jestli byl
        /// BackupTask zrušen, a pokud zjistí, že ano, tak pomocí goto přeskočí na kód pro dokončení zálohy.
        /// Na chytrosti tohoto zrušení jistě lze ještě zapracovat
        /// </summary>
        public void Cancel()
        {
            if (State == TaskState.Running)
                IsCancelled = true;
            else
                throw new InvalidOperationException("Nelze zrušit zálohu, která neprobíhá");
        }

    }

    public enum TaskState { NotStartedYet, Running, Finished, Aborted, Failed, Cancelled }
}
