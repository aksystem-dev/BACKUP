using SmartModulBackupClasses;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.ServiceProcess;
using System.Text;
using System.Threading;
//using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Runtime.InteropServices;
using Alphaleonis.Win32.Vss;
using SmartModulBackupClasses.Managers;
using SmartModulBackupClasses.WebApi;
using smart_modul_BACKUP_service.Managers;
using smart_modul_BACKUP_service.BackupExe;
using System.Reflection;

namespace smart_modul_BACKUP_service
{
    /// <summary>
    /// Zde je kód pro chování windows služby na pozadí
    /// </summary>
    public partial class SmartModulBackupService : ServiceBase
    {
        /// <summary>
        /// Interval, v kterém se bude automaticky znovu načítat konfigurace, pravidla, 
        /// a plánovat zálohy
        /// </summary>
        public static TimeSpan _scheduleInterval = new TimeSpan(0, 10, 0);

        const bool WAIT_ON_START = false;

        /// <summary>
        /// Časovač využívaný pro reload (načítání konfigurace, pravidel, plánování záloh, apod);
        /// </summary>
        private System.Timers.Timer timer;

        /// <summary>
        /// WCF služba, na kterou se napojí instance uživatelského rozhraní
        /// </summary>
        private WCF.SmartModulBackupInterface wcf_service;

        /// <summary>
        /// Hostitel služby
        /// </summary>
        private ServiceHost host;

        BackupTimeline timeline;
        FolderObserver observer;

        public SmartModulBackupService()
        {
            //Thread.Sleep(10000);

            //nastavit statický odkaz na tuto instanci
            Utils.Service = this;

            InitializeComponent();

            //porychtovat eventlog
            if (!EventLog.SourceExists("SmartModulBackupEvents"))
                EventLog.CreateEventSource("SmartModulBackupEvents", "SmartModulBackupLog");
            evlog.Source = "SmartModulBackupEvents";
            evlog.Log = "SmartModulBackupLog";
            //evlog.Clear();

            DumbLogger.eventLog = evlog;
            SMB_Log.OnLog += SMB_Log_OnLog;
        }

        /// <summary>
        /// SMB_Log je definovaný v projektu SmartModulBackupClasses, aby měly jeho třídy
        /// možnost psát do EventLogu, musíme se napojit na událost OnLog a předat zprávu
        /// Loggeru definovanému v SmartModulBackupService
        /// </summary>
        /// <param name="obj"></param>
        private void SMB_Log_OnLog(LogArgs obj)
        {
            switch(obj.Type)
            {
                case LogType.Error: 
                    DumbLogger.Error(obj.Message);
                    break;
                case LogType.Info: 
                    DumbLogger.Log(obj.Message); 
                    break;
                case LogType.Success: 
                    DumbLogger.Success(obj.Message); 
                    break;
                case LogType.Warning: 
                    DumbLogger.Warn(obj.Message); 
                    break;
            }
        }

        /// <summary>
        /// Kód pro spuštění při startu služby
        /// </summary>
        /// <param name="args"></param>
        protected override void OnStart(string[] args)
        {
            try
            {
                //pokud je konstanta wait == true, počkat 10 vteřin, aby se mohly napojit debugovací nástroje
                if (WAIT_ON_START)
                    Thread.Sleep(10000);

                //když dojde k výjimce, vypsat ji do eventlogu
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

                DumbLogger.Log("Služba smart modul BACKUP spuštěna!!");

                //pracujeme ve složce, kde je exe služby (ve stejné by mělo být exe GUI)
                string cd = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                Directory.SetCurrentDirectory(cd);

                //Manager umožňuje
                //  - uchovávat instance užitečných tříd a pak je dostávat podle typu (SetSingleton<T>, Get<T>)
                //  - vytvářet nové instance užitečných tříd vždy, když si o ní řekneme (SetTransient<T>, Get<T>)

                var loggingConfig = Manager.SetSingleton(new ConfigManager()).Load(out _).Config.Logging; //pracuje s config.xml
                SmbLog.Configure(loggingConfig, SmbAssembly.Service); //nastavit logger
                SmbLog.Info("Načtena konfigurace pro logování");
                Manager.SetTransient(new SqlBackuperFactory()); //SqlBackuper využívaný na SQL zálohy
                Manager.SetTransient(new SftpUploaderFactory()); //SftpUploader - obaluje SftpClient
                Manager.SetSingleton(new AccountManager()); //AccountManager - umožňuje získávat info o plánu
                Manager.SetSingleton(new RuleScheduler()); //RuleScheduler - plánuje pravidla
                Manager.SetSingleton(new BackupCleaner()); //BackupCleaner - odstraňuje staré zálohy
                Manager.SetSingleton(new Mailer()); //Mailer - umožňuje posílat maily
                Manager.SetSingleton(new SmbMailer()); //SmbMailer - umožňuje generovat a posílat maily specifické pro smart modul BACKUP
                observer = Manager.SetSingleton(new FolderObserver());

                ////Vytvořit Backuper - ten se stará o samotné zálohy
                //var backuper = new Backuper()
                //{
                //    TempDir = Path.Combine(Directory.GetCurrentDirectory(), "temp_dir")
                //};
                //Manager.SetSingleton(backuper);

                //Vytvořit BackupTimeline - ta se stará o spouštění záloh ve správné časy
                timeline = Manager.SetSingleton(new BackupTimeline());

                ////Vytvořit Restorer - ten se stará o obnovy (ty se nespouští automaticky, pouze přímo z GUI)
                //var restorer = new Restorer()
                //{
                //    TempDir = Path.Combine(Directory.GetCurrentDirectory(), "temp_dir_restore")
                //};
                //Manager.SetSingleton(restorer);

                Manager.SetSingleton(new BackupInfoManager()); //načítá info o provedených zálohách z lokální složky, webového api, popř. SFTP serveru
                Manager.SetSingleton(new BackupRuleLoader()); //načítá info o pravidlech, synchronizujíc je s webovým API

                //spustit wcf službu pro komunikaci s uživatelským rozhraním
                wcf_service = new WCF.SmartModulBackupInterface(this);
                host = new ServiceHost(wcf_service);
                host.Open();
                SmbLog.Info($"Hostuji WCF službu; host.State = {host.State}", null, LogCategory.ServiceHost);

                //zveřejnit objekt, který umožní interakci s GUI odkudkoliv z kódu
                Utils.GUIS = wcf_service.Callback;

                //když se něco pokazí, kváknout
                host.Closed += (_, __) => SmbLog.Warn("WCF služba ukončena?", null, LogCategory.ServiceHost);
                host.Faulted += (_, __) => SmbLog.Error("Došlo k chybě při komunikaci s GUI", null, LogCategory.ServiceHost);

                PeriodicLoad(() => Manager.Get<BackupInfoManager>().FixIDs().Wait());

                timer = new System.Timers.Timer();
                timer.Elapsed += Timer_Elapsed;
                timer.Interval = _scheduleInterval.TotalMilliseconds;
                timer.Start();
            }
            catch(Exception e)
            {
                DumbLogger.Ex(e);
                DumbLogger.Warn("Ukončuji službu");

                Stop();
            }
        }

        private void CreateSampleRule()
        {
            var rule = new BackupRule()
            {
                Enabled = false,
                LastExecution = DateTime.Now,
                LocalBackups = new BackupConfig()
                {
                    enabled = true,
                    MaxBackups = 3
                },
                RemoteBackups = new BackupConfig()
                {
                    enabled = true,
                    MaxBackups = 5
                },
                Name = "ExamplePravidlo",
                Sources = new BackupSourceCollection()
                {
                    Databases = new BackupSource[]{
                        new BackupSource()
                        {
                            path = "databaze"
                        } 
                    },
                    Directories = new BackupSource[]
                    {
                        new BackupSource()
                        {
                            path = "slozka"
                        }
                    }
                },
                Conditions = new Conditions()
                {
                    DayInWeek = "1-5",
                    Time = "08:00:00 - 16:00:00"
                },
                path = "Rules/example.xml"
            };

            rule.Sources.FixIds();
            rule.SaveSelf();
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = (e.ExceptionObject as Exception);
            SmbLog.Fatal("Došlo k neošetřené výjimce", ex);
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            SmbLog.Info("Časovač začasoval", null, LogCategory.Service);

            timer.Interval = _scheduleInterval.TotalMilliseconds;

            PeriodicLoad();
        }

        /// <summary>
        /// Metoda, která načte všechny důležité věci. Měla by se volat pravidelně.
        /// </summary>
        public void PeriodicLoad(Action afterBackupInfosLoaded = null)
        {

            //zastavit časovou osu
            if (timeline.Running)
                timeline.Stop();

            var load_called = DateTime.Now;

            //zastavit pozorovač složek
            observer.Stop();
            observer.Clear();

            //znovu načíst důležitou konfiguraci
            var cfg = Manager.Get<ConfigManager>().Load(out _);
            updateApi(cfg);
            Manager.Get<BackupRuleLoader>().Load();
            var bkman = Manager.Get<BackupInfoManager>();
            bkman.LoadAsync().Wait();
            //SMB_Utils.Sync(async () =>
            //{
            //    await bkman.LoadAsync();
            //});
            afterBackupInfosLoaded?.Invoke();

            //naplánovat pravidla
            DateTime plan_till = load_called + _scheduleInterval;
            var bk_tasks = Manager.Get<RuleScheduler>().GetBackupTaskList(load_called, plan_till);

            //odstranit staré zálohy
            Manager.Get<BackupCleaner>().CleanupAllRulesAsync();

            //spustit časovou osu
            timeline.Start(bk_tasks, plan_till);

            //spustit pozorovač složek
            observer.Start();

            //ověřit, zdali jsme stále připojeni k GUI
            Utils.GUIS.TestConnection();

            //odeslat maily k odeslání
            Manager.Get<Mailer>()?.SendPendingEmailsAsync();
        }

        protected override void OnStop()
        {
            if (timeline.Running)
                timeline.Stop();

            foreach (var task in BackupTask.RunningBackupTasks)
                task.Cancel();

            Task.WhenAll(BackupTask.RunningBackupTasks.Select(bt => bt.TheTask)).Wait();

            if (host != null && host.State == CommunicationState.Opened)
            {
                SmbLog.Info("Ukončuji komunikaci s rozhraním", null, LogCategory.ServiceHost);
                try
                {
                    Utils.GUIS.Goodbye();
                }
                catch (Exception ex)
                {
                    SmbLog.Error("Chyba při ukončování WCF komunikace s GUI", ex, LogCategory.ServiceHost);
                }
                host.Close();
                host = null;
            }

            DumbLogger.Log("Služba smart modul BACKUP stopnuta");
        }

        /// <summary>
        /// Znovu se přihlásí do AccountManageru
        /// </summary>
        /// <param name="cfg"></param>
        public static void updateApi(ConfigManager cfg)
        {
            try
            {
                Manager.Get<AccountManager>().LoginWithAsync(cfg?.Config?.WebCfg).Wait();
            }
            catch { }
        }
    }
}
