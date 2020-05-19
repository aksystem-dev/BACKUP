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

namespace smart_modul_BACKUP_service
{
    public partial class SmartModulBackupService : ServiceBase
    {
        //[DllImport("advapi32.dll", SetLastError = true)]
        //private static extern bool SetServiceStatus(System.IntPtr handle, ref ServiceStatus serviceStatus);

        static TimeSpan _scheduleInterval = new TimeSpan(0, 10, 0);

        private System.Timers.Timer timer;
        //private TimeSpan timerInterval = new TimeSpan(1, 0, 0, 0);
        public List<BackupRule> rules = new List<BackupRule>();

        public Backuper backuper;
        public Restorer restorer;
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

                //Vytvořit Backuper
                backuper = new Backuper()
                {
                    TempDir = Path.Combine(Directory.GetCurrentDirectory(), "temp_dir")
                };

                //Vytvořit BackupTimeline
                timeline = new BackupTimeline(backuper);

                //Vytvořit Restorer
                restorer = new Restorer()
                {
                    TempDir = Path.Combine(Directory.GetCurrentDirectory(), "temp_dir_restore")
                };

                //načte konfiguraci, spustí časovač, naplánuje pravidla
                Reload();

                //načítač info o proběhnutých zálohách - momentálně se o nich info ukládá do xml
                Utils.SavedBackups = new XmlInfoLoaderSftpMirror<Backup>("saved_backups.xml", Utils.SftpFactory,
                    remoteFile: Path.Combine(Utils.Config.RemoteBackupDirectory, "saved_backups.xml"));
                Utils.SavedBackups.LoadInfos();

                //spustit wcf službu pro komunikaci s uživatelským rozhraním
                wcf_service = new WCF.SmartModulBackupInterface(this);
                host = new ServiceHost(wcf_service);
                host.Open();
                Logger.Log($"Hostuji WCF službu; host.State = {host.State}");

                host.Closed += (_, __) => Logger.Warn("WCF služba ukončena?");
                host.Faulted += (_, __) => Logger.Error("chyba při komunikaci s gui");

                //zveřejnit objekt, který umožní interakci s GUI odkudkoliv z kódu
                Utils.GUIS = wcf_service.Callback;
                //Utils.gui = new GUI(wcf_service);

                //status.dwCurrentState = ServiceState.SERVICE_RUNNING;
                //SetServiceStatus(this.ServiceHandle, ref status);
            }
            catch(Exception e)
            {
                Logger.Error($"Problém při spouštění služby ({e.GetType().Name})\n\n{e.Message}");
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
                Logger.Error($"Chyba s časovačem:\n{e.Message}");
                Stop();
            }
        }

        /// <summary>
        /// Načte soubor konfigurace. Nepovede-li se to, služba se vypne.
        /// </summary>
        private void LoadConfig(bool stopOnFailure = true)
        {
            try
            {
                if (!File.Exists("config.xml"))
                    NewConfig();
                else
                    try 
                    {
                        //logger.WriteEntry("current dir: " + Directory.GetCurrentDirectory());
                        //logger.WriteEntry("full path: " + Path.GetFullPath("config.xml"));
                        string xml = File.ReadAllText("config.xml");
                        Logger.Log("xml contents: " + xml);
                        Utils.Config = Config.FromXML(xml);
                        File.WriteAllText("config.xml", Utils.Config.ToXML());
                    }
                    catch
                    {
                        NewConfig();
                    }

                Logger.Log("Konfigurace načtena");

            }
            catch (Exception e)
            {
                Logger.Error($"Chyba s načítáním konfigurace\n {e.GetType().Name} \n {e.Message}");

                if (stopOnFailure)
                    Stop();
            }
        }

        /// <summary>
        /// Načte pravidla ze souborů
        /// </summary>
        private void LoadRules()
        {
            Logger.Log("Načítám pravidla");

            rules = new List<BackupRule>();
            if (!Directory.Exists("Rules"))
                Directory.CreateDirectory("Rules");
            else
            {
                var files = Directory.GetFiles("Rules", "*.xml");
                //načíst pravidla ze souborů
                if (files.Length > 0)
                    foreach (string rulepath in files)
                    {
                        //if (Path.GetExtension(rulepath) == ".xml")
                        {
                            try
                            {
                                var rule = BackupRule.LoadFromXml(rulepath);

                                if (rules.Any(f => f.Name == rule.Name))
                                    Logger.Warn($"Pravidlo s názvem {rule.Name} se mezi pravidly vyskytlo vícekrát. Beru pouze první výskyt");

                                //zajistit, aby mělo pravidlo unikátní id
                                //pokud vše funguje jak má (GUI), rule.Id++ by se nikdy volat nemělo
                                while (rules.Any(f => f.LocalID == rule.LocalID))
                                    rule.LocalID++;

                                //možná jsme provedli nějaké změny (změnili id pravidla, ...), ty je třeba uložit
                                rule.SaveSelf();

                                //pravidlo přidáme na seznam, pokud je validní
                                if (rule.Conditions.AllValid)
                                    rules.Add(rule);
                            }
                            catch (Exception e)
                            {
                                Logger.Error($"Nepodařilo se načíst pravidlo umístěné v {rulepath}. Přeskakuji ho.\n\n{e}");
                            }
                        }
                    }
                //else
                //    CreateSampleRule();
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

        /// <summary>
        /// Vytvoří soubor konfigurace.
        /// </summary>
        private void NewConfig()
        {
            Logger.Log("Vytvářím soubor konfigurace");

            Utils.Config = new Config();

            Utils.Config.Connection = new DatabaseConfig();

            File.WriteAllText("config.xml", Utils.Config.ToXML());
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = (e.ExceptionObject as Exception);
            Logger.Error($"Došlo k výjimce \n {ex.GetType().Name} \n {ex.Message} \n\n Zásobník:\n{ex.StackTrace} ");
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Logger.Log("Časovač začasoval");

            //backuper.Backup(config.BackupDatabase, config.BackupPath);

            LoadConfig();
            LoadRules();

            timer.Interval = _scheduleInterval.TotalMilliseconds;

            if (timeline.Running)
                timeline.Stop();

            var backupTasks = ScheduleRules(_scheduleInterval);
            timeline.Start(backupTasks);

            Utils.GUIS.TestConnection();
        }

        /// <summary>
        /// Spustí asynchronní úlohy pro pravidla v intervalu
        /// </summary>
        /// <param name="forHowLong">Pravidla jsou plánována od DateTime.Now po DateTime.Now + forHowLong</param>
        private List<BackupTask> ScheduleRules(TimeSpan forHowLong)
        {
            int total = 0;

            DateTime start = DateTime.Now;
            DateTime end = start + forHowLong;

            Logger.Log($"Plánuji pravidla mezi {start} a {end}");

            List<BackupTask> backupTasks = new List<BackupTask>();

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
            if (loadConfig)
                LoadConfig();
            if (loadRules)
                LoadRules();

            //vytvořit instanci SqlBackuperFactory, kterou bude využívat Backuper
            if (loadSql)
                Utils.SqlFactory = new SqlBackuperFactory(Utils.Config.Connection.GetConnectionString(2));

            //vytvořit instanci SftpUploaderFactory, kterou bude využívat Backuper
            if (loadSftp)
                Utils.SftpFactory = new SftpUploaderFactory(Utils.Config.SFTP);

            //naplánovat pravidla
            if (scheduleRules)
            {
                var backupTasks = ScheduleRules(_scheduleInterval);
                timeline.Start(backupTasks);
            }

            //spustit časovač, který bude plánovat pravidla v daném intervalu
            if (startTimer)
                StartTimer((int)_scheduleInterval.TotalMilliseconds);
        }
    }
}
