using smart_modul_BACKUP.Models;
using SmartModulBackupClasses;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;
using System.Xml.Serialization;

namespace smart_modul_BACKUP
{
    /// <summary>
    /// Třída obsahující načtenou konfiguraci; umožňuje její načtění a uložení
    /// </summary>
    public static class LoadedStatic
    {
        public static ObservableCollection<BackupRule> rules { get; set; } = new ObservableCollection<BackupRule>();

        public static ObservableCollection<AvailableDatabase> availableDatabases { get; set; }
            = new ObservableCollection<AvailableDatabase>();
        public static Config config { get; set; } = new Config();
        public static XmlInfoLoaderSftpMirror<Backup> SavedBackupsLoader;// = new XmlInfoLoaderSftpMirror<Backup>("saved_backups.xml");
        public static ObservableCollection<Backup> SavedBackups { get; set; } = new ObservableCollection<Backup>();
        public static ServiceState service { get; set; } = new ServiceState();
        public static InProgress InProgress { get; set; } = new InProgress();
        public static NotifyIcon notifyIcon;
        public static SftpUploaderFactory sftpFactory;

        private static string configFile = "config.xml";
        private static string dbsFile = "dbs.xml";
        public static readonly string savedBackupsFile = "saved_backups.xml";

        public static event Action beforeSave;
        public static event Action afterSave;

        public static void MSG(string msg, string title = "Info", ToolTipIcon icon = ToolTipIcon.Info)
            => notifyIcon?.ShowBalloonTip(2000, title, msg, icon);

        public static bool Loaded { get; private set; }

        public static void Load()
        {
            LoadRules();
            LoadConfig();
            LoadAvailableDatabases();
            LoadSavedBackups();

            Loaded = true;
        }

        public static void SaveAll()
        {
            beforeSave?.Invoke();
            SaveConfig();

            foreach (var rule in rules)
                rule.SaveSelf();

            afterSave?.Invoke();
        }

        public static void SaveConfig()
        {
            File.WriteAllText(configFile, config.ToXML());
        }

        public static void LoadRules()
        {
            rules.Clear();

            if (!Directory.Exists("Rules"))
                Directory.CreateDirectory("Rules");
            else
            {
                var files = Directory.GetFiles("Rules");
                //načíst pravidla ze souborů
                if (files.Length > 0)
                    foreach (string rulepath in files)
                    {
                        if (Path.GetExtension(rulepath) == ".xml")
                        {
                            var rule = BackupRule.LoadFromXml(rulepath, true);

                            //rule.TAG = new ProgBarsTag();

                            rules.Add(rule);
                        }
                    }
            }
        }

        public static void LoadConfig()
        {
            if (File.Exists(configFile))
                try
                {
                    config = Config.FromXML(File.ReadAllText(configFile));
                }
                catch
                {
                    config = new Config();
                }
            else
                config = new Config();

            sftpFactory = new SftpUploaderFactory(config.SFTP);
        }

        public static void LoadAvailableDatabases()
        {
            availableDatabases.Clear();

            using (var logfile = new StreamWriter("db_load_log.txt"))
            {
                //nyní se připojíme k serveru a zjistíme, jestli jsou nějaké databáze, o kterých ještě nevíme
                using (var conn = new SqlConnection(config.Connection.GetConnectionString(1)))
                {
                    try
                    {
                        conn.Open();

                        //nejprve stáhneme názvy všech databází
                        SqlCommand com = new SqlCommand("USE master; SELECT name FROM sys.databases", conn);
                        logfile.WriteLine($"posílám sql příkaz \"{com.CommandText}\"");

                        var reader = com.ExecuteReader();
                        while (reader.Read())
                            //nechceme databázi tempdb, páč tu nelze zálohovat
                            if (((string)reader[0]).ToLower() != "tempdb")
                            {
                                string dbname = reader[0] as string;

                                logfile.WriteLine($"načtena databáze {dbname}");

                                if (!availableDatabases.Any(f => f.name == dbname))
                                    availableDatabases.Add(new AvailableDatabase() { firma = null, name = dbname });
                            }
                        reader.Close();

                        logfile.WriteLine("Všechny načtené databáze:");
                        foreach (var d in availableDatabases)
                            logfile.WriteLine($"    - {d.name}");

                        com.Dispose(); //vyplivnout objekt SqlCommand

                        //pokud StwPh_sys neexistuje, jsme hotovi
                        if (!availableDatabases.Any(f => f.name.ToLower() == "stwph_sys"))
                        {
                            logfile.WriteLine("nevidím databázi StwPh_sys, nebudu se v ní tedy hrabat");
                            return;
                        }

                        //pokud StwPh_sys existuje, projdeme jí a načteme firmy přidružené k jednotlivým databázím
                        com = new SqlCommand("USE StwPh_sys; SELECT Soubor, Firma FROM Firma", conn);
                        logfile.WriteLine($"posílám sql příkaz \"{com.CommandText}\"");
                        reader = com.ExecuteReader();

                        logfile.WriteLine("příkaz spuštěn");

                        while (reader.Read())
                        {
                            logfile.WriteLine($"řádek v StwPh_sys");

                            string dbname = reader[0] as string;
                            string firma = reader[1] as string;

                            logfile.WriteLine($"zjištěno, že databáze {dbname} patří k firmě {firma}");

                            var corresponding = availableDatabases.Where(f => f.name.ToLower() == dbname.ToLower());

                            if (!corresponding.Any())
                                logfile.WriteLine($"v seznamu načtených db nenalezena databáze {dbname}");
                            else
                                logfile.WriteLine($"načítám názvy firem");

                            corresponding.ForEach(f => f.firma = firma);
                        }

                        reader.Close();

                        conn.Close();
                    }
                    catch (Exception e)
                    {
                        logfile.WriteLine($"\n!!!!!!!\n{e.GetType().Name}\n{e.Message}\n");
                    }
                }
            }
        }

        public static void LoadSavedBackups()
        {
            if (SavedBackupsLoader == null)
                SavedBackupsLoader = new XmlInfoLoaderSftpMirror<Backup>(savedBackupsFile, sftpFactory,
                    remoteFile: Path.Combine(config.RemoteBackupDirectory, "saved_backups.xml"));

            SavedBackupsLoader.LoadInfos();
            SavedBackups.Clear();
            SavedBackupsLoader.GetInfos().ForEach(f =>
            {
                //f.TAG = new ProgBarsTag();
                SavedBackups.Add(f);
            });
        }
    }
}
