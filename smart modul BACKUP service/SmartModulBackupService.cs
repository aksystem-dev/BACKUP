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

namespace smart_modul_BACKUP_service
{
    public partial class SmartModulBackupService : ServiceBase
    {
        public static TimeSpan _scheduleInterval = new TimeSpan(0, 10, 0);

        private System.Timers.Timer timer;
        private WCF.SmartModulBackupInterface wcf_service;
        private ServiceHost host;

        public BackupTimeline timeline;

        public SmartModulBackupService()
        {
            //nastavit statický odkaz na tuto instanci
            Utils.Service = this;

            InitializeComponent();

            //porychtovat eventlog
            if (!EventLog.SourceExists("SmartModulBackupEvents"))
                EventLog.CreateEventSource("SmartModulBackupEvents", "SmartModulBackupLog");
            evlog.Source = "SmartModulBackupEvents";
            evlog.Log = "SmartModulBackupLog";
            evlog.Clear();

            //když někdo pošle zprávu přes SMB_Log, předáme jí Loggeru
            SMB_Log.OnLog += SMB_Log_OnLog;

            Logger.eventLog = evlog;
        }

        private void SMB_Log_OnLog(LogArgs obj)
        {
            switch(obj.Type)
            {
                case LogType.Error: 
                    Logger.Error(obj.Message);
                    break;
                case LogType.Info: 
                    Logger.Log(obj.Message); 
                    break;
                case LogType.Success: 
                    Logger.Success(obj.Message); 
                    break;
                case LogType.Warning: 
                    Logger.Warn(obj.Message); 
                    break;
            }
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                //když dojde k výjimce, vypsat ji do eventlogu
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

                Logger.Log("Služba smart modul BACKUP spuštěna!!");

                //cd do složky, kam chceme
                string cd = "C:\\smart modul BACKUP";
                if (!Directory.Exists(cd)) Directory.CreateDirectory(cd);
                Directory.SetCurrentDirectory(cd);
                Logger.Log($"Aktuální adresa: {cd}");

                Manager.SetSingleton(new ConfigManager()); //pracuje s config.xml
                Manager.SetTransient(new SqlBackuperFactory()); //SqlBackuper využívaný na SQL zálohy
                Manager.SetTransient(new SftpUploaderFactory()); //SftpUploader - obaluje SftpClient
                Manager.SetSingleton(new PlanManager()); //PlanManager - umožňuje získávat info o plánu
                Manager.SetSingleton(new RuleScheduler()); //RuleScheduler - plánuje pravidla

                //Vytvořit Backuper - ten se stará o samotné zálohy
                var backuper = new Backuper()
                {
                    TempDir = Path.Combine(Directory.GetCurrentDirectory(), "temp_dir")
                };
                Manager.SetSingleton(backuper);

                //Vytvořit BackupTimeline - ta se stará o spouštění záloh ve správné časy
                timeline = Manager.SetSingleton(new BackupTimeline());

                //Vytvořit Restorer - ten se stará o obnovy (ty se nespouští automaticky, pouze přímo z GUI)
                var restorer = new Restorer()
                {
                    TempDir = Path.Combine(Directory.GetCurrentDirectory(), "temp_dir_restore")
                };
                Manager.SetSingleton(restorer);

                Manager.SetSingleton(new BackupInfoManager());
                Manager.SetSingleton(new BackupRuleLoader());

                Reload(); //načte všechna důležitá data jakož konfiguraci, pravidla, apod.

                //spustit wcf službu pro komunikaci s uživatelským rozhraním
                wcf_service = new WCF.SmartModulBackupInterface(this);
                host = new ServiceHost(wcf_service);
                host.Open();
                Logger.Log($"Hostuji WCF službu; host.State = {host.State}");

                host.Closed += (_, __) => Logger.Warn("WCF služba ukončena?");
                host.Faulted += (_, __) => Logger.Error("Došlo k chybě při komunikaci s GUI");


                //zveřejnit objekt, který umožní interakci s GUI odkudkoliv z kódu
                Utils.GUIS = wcf_service.Callback;
                //Utils.gui = new GUI(wcf_service);

                //status.dwCurrentState = ServiceState.SERVICE_RUNNING;
                //SetServiceStatus(this.ServiceHandle, ref status);
            }
            catch(Exception e)
            {
                Logger.Ex(e);
                Logger.Warn("Ukončuji službu");

                Stop();
            }
        }

        /// <summary>
        /// Poté, co se lokálně uloží xml s informacemi o zálohách, je chceme zkopírovat na sftp
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Backups_OnInfosSaved(object sender, InfoSaveEventArgs<Backup> e)
        {
            
        }

        /// <summary>
        /// Spustí časovač s daným intervalem. Nepovede-li se to, služba se vypne.
        /// </summary>
        /// <param name="interval"></param>
        private void StartTimer(int interval)
        {
            try
            {
                timer = new System.Timers.Timer();
                timer.Interval = interval;
                timer.Elapsed += Timer_Elapsed;
                timer.Start();

                Logger.Log("Časovač spuštěn; první tik za " + interval + "ms ");
            }
            catch (Exception e)
            {
                Logger.Ex(e);

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

        ///// <summary>
        ///// Vytvoří soubor konfigurace.
        ///// </summary>
        //private void NewConfig()
        //{
        //    Logger.Log("Vytvářím soubor konfigurace");

        //    Utils.Config = new Config();

        //    Utils.Config.Connection = new DatabaseConfig();

        //    File.WriteAllText("config.xml", Utils.Config.ToXML());
        //}

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = (e.ExceptionObject as Exception);
            Logger.Ex(ex);
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Logger.Log("Časovač začasoval");

            timer.Interval = _scheduleInterval.TotalMilliseconds;

            //zastavit časovou osu
            if (timeline.Running)
                timeline.Stop();

            //znovu načíst důležitou konfiguraci
            Manager.Get<ConfigManager>().Load();
            Manager.Get<BackupRuleLoader>().Load();

            //naplánovat pravidla a spustit časovou osu
            Manager.Get<RuleScheduler>().ScheduleRules(_scheduleInterval);

            //ověřit, zdali jsme stále připojeni k GUI
            Utils.GUIS.TestConnection();
        }


        protected override void OnStop()
        {
            if (host != null && host.State == CommunicationState.Opened)
            {
                Logger.Log("Ukončuji komunikaci s rozhraním");
                Utils.GUIS.Goodbye();
                host.Close();
                host = null;
            }

            if (timeline.Running)
                timeline.Stop();

            Logger.Log("Služba smart modul BACKUP stopnuta");
        }

        /// <summary>
        /// Znovu načte službu (znovu načte konfigurační soubory a podle nich naplánuje pravidla. Zálohy, které již probíhají, se nezruší.)
        /// </summary>
        public void Reload(bool loadSftp = true, bool loadSql = true, bool loadConfig = true, bool loadRules = true, bool scheduleRules = true, bool startTimer = true)
        {
            //pokud časovač běží, vypneme ho
            if (timer != null && timer.Enabled)
                timer.Stop();

            //pokud timeline běží, vypneme jí, aby se nestalo, že se jedno pravidlo spustí v jeden moment vícekrát
            if (timeline.Running)
                timeline.Stop();

            //načíst vše potřebné
            var cfg = Manager.Get<ConfigManager>();
            if (loadConfig)
            {
                cfg.Load();

                updateApi(cfg);
            }
            if (loadRules)
                Manager.Get<BackupRuleLoader>().Load();

            //naplánovat pravidla
            if (scheduleRules)
            {
                Manager.Get<RuleScheduler>().ScheduleRules(_scheduleInterval);
            }

            //spustit časovač, který bude plánovat pravidla v daném intervalu
            if (startTimer)
                StartTimer((int)_scheduleInterval.TotalMilliseconds);
        }

        /// <summary>
        /// Vytvoří novou instanci SmbApiClient a znovu načte PlanManager.
        /// </summary>
        /// <param name="cfg"></param>
        public static void updateApi(ConfigManager cfg)
        {
            //pokud se načetla konfigurace, mohlo se změnit nastavení připojení k api; to tedy také znovu načteme
            if (!cfg.Config.WebCfg.Online)
                Manager.SetSingleton<SmbApiClient>(null); //jsme-li offline, připojení nemáme
            else
                Manager.SetSingleton(new SmbApiClient(cfg.Config.WebCfg));
            Manager.Get<PlanManager>().Load(); //znovu načíst info o plánu
        }
    }
}
