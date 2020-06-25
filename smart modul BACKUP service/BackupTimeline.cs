using smart_modul_BACKUP_service.BackupExe;
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
    /// Spouští BackupTasky jeden po druhém v naplánované časy. Běží na vlastním vlákně.
    /// </summary>
    public class BackupTimeline
    {
        public bool Running { get; private set; } = false;

        private static List<BackupTask> _ongoingBackups = new List<BackupTask>();
        public static BackupTask[] OngoingBackups => _ongoingBackups.ToArray();

        private CancellationTokenSource _tokenSource;
        private Thread _loopThread;

        public BackupTimeline() { }

        /// <summary>
        /// Ukončí vyhodnocování dalších pravidel.
        /// </summary>
        public void Stop()
        {
            //timeline lze stopnout pouze pokud běží, tedy Running == true
            if (Running)
            {
                _tokenSource.Cancel(); //říct vláknu, že je konec
                Running = false; //říct objektu, že je konec
                _loopThread?.Join();
            }
            else
                throw new InvalidOperationException("BackupTimeline neběží, nelze jí tedy zastavit, z logiky věci přece.");
        }

        public void RescheduleRule(BackupRule rule)
        {
            if (Running)
            {
                _tokenSource.Cancel();
                _loopThread?.Join();
            }

            try
            {
                _tasks.RemoveAll(bkt => bkt.Rule.LocalID == rule.LocalID);
                _tasks.AddRange(rule.GetBackupTasksInTimeSpan(DateTime.Now, End));
                _tasks = _tasks.OrderBy(bkt => bkt.ScheduledStart).ToList();
            }
            catch (Exception ex)
            {
                SMB_Log.LogEx(ex);
            }

            if (Running)
            {
                _tokenSource = new CancellationTokenSource();
                _loopThread = new Thread(Loop);
                _loopThread.Start();
            }
        }

        private List<BackupTask> _tasks;

        public DateTime End { get; private set; }

        /// <summary>
        /// Spustí Timeline s danými BackupTasky
        /// </summary>
        /// <param name="tasks"></param>
        public void Start(IEnumerable<BackupTask> tasks, DateTime end)
        {
            //lze startovat pouze pokud to již neběží
            if (!Running)
            {
                End = end;

                Logger.Log("Spuštěno BackupTimeline");

                //nastavit _tasks, ale musíme dát pozor, aby to šlo chronologicky
                _tasks = tasks.OrderBy(f => f.ScheduledStart).ToList();

                Logger.Log($"Naplánované úlohy v časech: {String.Join(", ", (_tasks as IEnumerable<BackupTask>).Select(f => f.ScheduledStart))}");

                //vysvětlit objektu, že od teď probíhá vyhodnocování
                Running = true;

                //vytvořit CancellationTokenSource, aby bylo možné zrušit cyklus
                _tokenSource = new CancellationTokenSource();

                //spustit cyklus na novém vlákně
                _loopThread = new Thread(Loop);
                _loopThread.Start();
            }
            else
                throw new InvalidOperationException("BackupTimeline už běží! Použijte Stop() pro její zastavení, než zavoláte Start().");
        }

        //tahle metoda se bude spouštět na jiném vlákně a postupně vyhodnocovat backuptasky
        private void Loop()
        {
            foreach (var backup in _tasks)
            {
                try
                {
                    if (!backup.Rule.Enabled)
                    {
                        Logger.Log($"Pravidlo {backup.Rule.Name} je naplánováno, ale zakázáno. Budu ho tedy ignorovat.");
                        continue;
                    }

                    //pokud je tam pravidlo, jehož naplánované  datum spuštění je dřív než teď, spustíme ho hned
                    if (backup.ScheduledStart <= DateTime.Now)
                    {
                        Logger.Log($"BackupTimeline: pravidlo {backup.Rule.Name} mělo být spuštěno již v {backup.ScheduledStart}. Jdu na to hned.");
                        backup.Execute();
                    }
                    //jinak (pokud je třeba ho vyhodnotit někday v budoucnu)
                    else
                    {
                        //počkáme, dokud nenastane čas na aktuální pravidlo
                        //pravidla by měla být seřazena už předem, čili toto by mělo fungovat
                        Logger.Log($"BackupTimeline: Čekám do {backup.ScheduledStart}");
                        Task.Delay(backup.ScheduledStart - DateTime.Now, _tokenSource.Token).Wait();

                        //pokud jsme to v mezičase zrušili, utečem z cyklu
                        if (_tokenSource.Token.IsCancellationRequested)
                            break;

                        //jinak pokračujeme spuštěním daného pravidla:
                        backup.Execute();
                    }
                }
                catch (Exception e) when
                    (e is TaskCanceledException
                    || e is OperationCanceledException
                    || (e is AggregateException a
                        && a.InnerExceptions.Any(f => f is TaskCanceledException || f is OperationCanceledException)))
                {
                    //pokud došlo k OperationCancelledException, musela ho způsobit metoda Stop(), která sama
                    //nastavuje Running na false, čili prostě zavoláme return a skončíme metodu a tedy i vlákno
                    Logger.Log("Vlákno BackupTimeline zrušeno");
                    return;
                }
                catch (Exception e)
                {
                    //jinak se pravděpodobně jedná jen o problém s jedním konkrétním pravidlem, vypíšeme to tedy
                    //a pokračujem
                    Logger.Ex(e);
                }
            }

            Logger.Log("BackupTimeline hotova.");
            Running = false;
        }

        ///// <summary>
        ///// Spustí daný BackupTask a přidá ho do seznamu
        ///// </summary>
        ///// <param name="task"></param>
        //public Task ExecuteAndAddToList(BackupTask task, BackupInProgress progress = null)
        //{
        //    //přidat ho do seznamu
        //    _ongoingBackups.Add(task);

        //    //spustit backup task
        //    return task.Execute(Backuper, progressbar)
        //        .ContinueWith((t) => //až bude hotov,
        //    {
        //        lock (_ongoingBackups)
        //            _ongoingBackups.Remove(task); //odstraníme ho ze seznamu
        //    });
        //}
    }
}
