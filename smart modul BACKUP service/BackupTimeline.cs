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

        private static void logError(string error, Exception ex)
        {
            SmbLog.Error(error, ex, LogCategory.BackupTimeline);
        }

        private static void logInfo(string info, Exception ex = null)
        {
            SmbLog.Info(info, ex, LogCategory.BackupTimeline);
        }

        public void Stop()
        {
            lock (this)
            {
                stop();
            }
        }

        /// <summary>
        /// Ukončí vyhodnocování dalších pravidel.
        /// </summary>
        private void stop()
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

        /// <summary>
        /// Zastaví BackupTimeline, odstraní BackupTasky daného pravidla, naplánuje je znovu, a znovu spustí BackupTimeline.
        /// </summary>
        /// <param name="rule"></param>
        public void RescheduleRule(BackupRule rule)
        {
            lock (this)
            {
                //zastavit timeline
                if (Running)
                    stop();

                try
                {
                    var now = DateTime.Now;

                    //odstranit BackupTasky, které
                    //   a) jsou k danému pravidlu
                    //   b) už byly spuštěny
                    _tasks.RemoveAll(bkt => bkt.Rule.LocalID == rule.LocalID || bkt.ScheduledStart < now);

                    //znovu naplánovat BackupTasky daného pravidla
                    _tasks.AddRange(rule.GetBackupTasksInTimeSpan(now, End));
                }
                catch (Exception ex)
                {
                    SmbLog.Error($"Problém při přeplánování pravidla {rule.Name}", ex, LogCategory.BackupTimeline);
                }

                //znovu spustit BackupTimeline
                if (!Running)
                    start(_tasks, End);
            }
        }

        private List<BackupTask> _tasks;

        public DateTime End { get; private set; }

        public void Start(IEnumerable<BackupTask> tasks, DateTime end)
        {
            lock(this)
            {
                start(tasks, end);
            }
        }

        /// <summary>
        /// Spustí Timeline s danými BackupTasky
        /// </summary>
        /// <param name="tasks"></param>
        private void start(IEnumerable<BackupTask> tasks, DateTime end)
        {
            //lze startovat pouze pokud to již neběží
            if (!Running)
            {
                End = end;

                logInfo("Spuštěno BackupTimeline");

                //nastavit _tasks, ale musíme dát pozor, aby to šlo chronologicky
                _tasks = tasks.OrderBy(f => f.ScheduledStart).ToList();

                logInfo($"Naplánované úlohy v časech: {String.Join(", ", (_tasks as IEnumerable<BackupTask>).Select(f => f.ScheduledStart))}");

                //vysvětlit objektu, že od teď probíhá vyhodnocování
                Running = true;

                //vytvořit CancellationTokenSource, aby bylo možné zrušit cyklus
                _tokenSource = new CancellationTokenSource();

                //spustit cyklus na novém vlákně
                _loopThread = new Thread(loop);
                _loopThread.Start();
            }
            else
                throw new InvalidOperationException("BackupTimeline už běží! Použijte Stop() pro její zastavení, než zavoláte Start().");
        }

        //tahle metoda se bude spouštět na jiném vlákně a postupně vyhodnocovat backuptasky
        private void loop()
        {
            foreach (var backup in _tasks)
            {
                try
                {
                    if (!backup.Rule.Enabled)
                    {
                        logInfo($"Pravidlo {backup.Rule.Name} je naplánováno, ale zakázáno. Budu ho tedy ignorovat.");
                        continue;
                    }

                    //pokud je tam pravidlo, jehož naplánované  datum spuštění je dřív než teď, spustíme ho hned
                    if (backup.ScheduledStart <= DateTime.Now)
                    {
                        logInfo($"BackupTimeline: pravidlo {backup.Rule.Name} mělo být spuštěno již v {backup.ScheduledStart}. Jdu na to hned.");
                        backup.Execute();
                    }
                    //jinak (pokud je třeba ho vyhodnotit někday v budoucnu)
                    else
                    {
                        //počkáme, dokud nenastane čas na aktuální pravidlo
                        //pravidla by měla být seřazena už předem, čili toto by mělo fungovat
                        logInfo($"BackupTimeline: Čekám do {backup.ScheduledStart}");
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
                    logInfo("Vlákno BackupTimeline zrušeno");
                    return;
                }
                catch (Exception e)
                {
                    //jinak se pravděpodobně jedná jen o problém s jedním konkrétním pravidlem, vypíšeme to tedy
                    //a pokračujem
                    logError("Došlo k chybě v BackupTimeline", e);
                }
            }

            logInfo("BackupTimeline hotova.");
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
