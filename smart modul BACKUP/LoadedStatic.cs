using smart_modul_BACKUP.Models;
using SmartModulBackupClasses;
using SmartModulBackupClasses.Managers;
using SmartModulBackupClasses.WebApi;
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
    //public static class LoadedStatic
    //{
    //    public static ObservableCollection<BackupRule> rules { get; set; } = new ObservableCollection<BackupRule>();

    //    public static ObservableCollection<AvailableDatabase> availableDatabases { get; set; }
    //        = new ObservableCollection<AvailableDatabase>();
    //    //public static Config config { get; set; } = new Config();
    //    public static XmlInfoLoaderSftpMirror<Backup> SavedBackupsLoader;// = new XmlInfoLoaderSftpMirror<Backup>("saved_backups.xml");
    //    public static ObservableCollection<Backup> SavedBackups { get; set; } = new ObservableCollection<Backup>();
    //    public static ServiceState service { get; set; } = new ServiceState();
    //    public static InProgress InProgress { get; set; } = new InProgress();
    //    public static NotifyIcon notifyIcon;
    //    public static SftpUploaderFactory sftpFactory;
    //    public static SmbApiClient API;
    //    public static BackupRuleLoader Rules;

    //    public static PlanXml ActivePlan { get; set; }

    //    private static string configFile = "config.xml";
    //    private static string dbsFile = "dbs.xml";
    //    public static readonly string savedBackupsFile = "saved_backups.xml";

    //    public static event Action beforeSave;
    //    public static event Action afterSave;

    //    public static void MSG(string msg, string title = "Info", ToolTipIcon icon = ToolTipIcon.Info)
    //        => notifyIcon?.ShowBalloonTip(2000, title, msg, icon);

    //    public static bool Loaded { get; private set; }

    //    public static void Load()
    //    {
    //        LoadRules();
    //        LoadConfig();
    //        LoadAvailableDatabases();
    //        LoadSavedBackups();

    //        Loaded = true;
    //    }

    //    public static void SaveAll()
    //    {
    //        beforeSave?.Invoke();
    //        SaveConfig();

    //        foreach (var rule in rules)
    //            rule.SaveSelf();

    //        afterSave?.Invoke();
    //    }

    //    public static void SaveConfig()
    //    {
    //        File.WriteAllText(configFile, config.ToXML());
    //    }

    //    public static void LoadRules()
    //    {
    //        rules.Clear();

    //        if (!Directory.Exists("Rules"))
    //            Directory.CreateDirectory("Rules");
    //        else
    //        {
    //            var files = Directory.GetFiles("Rules");
    //            //načíst pravidla ze souborů
    //            if (files.Length > 0)
    //                foreach (string rulepath in files)
    //                {
    //                    if (Path.GetExtension(rulepath) == ".xml")
    //                    {
    //                        try
    //                        {
    //                            var rule = BackupRule.LoadFromXml(rulepath, true);

    //                            //rule.TAG = new ProgBarsTag();

    //                            rules.Add(rule);
    //                        }
    //                        catch
    //                        {

    //                        }
    //                    }
    //                }
    //        }
    //    }

    //    public static void LoadConfig()
    //    {
    //        if (File.Exists(configFile))
    //            try
    //            {
    //                config = Config.FromXML(File.ReadAllText(configFile));
    //            }
    //            catch
    //            {
    //                config = new Config();
    //            }
    //        else
    //            config = new Config();

    //        sftpFactory = new SftpUploaderFactory(config.SFTP);
    //    }

    //    public static void LoadAvailableDatabases()
    //    {

    //    }

    //    public static void LoadSavedBackups()
    //    {
    //        if (SavedBackupsLoader == null)
    //            SavedBackupsLoader = new XmlInfoLoaderSftpMirror<Backup>(savedBackupsFile, sftpFactory,
    //                remoteFile: Path.Combine(config.RemoteBackupDirectory, "saved_backups.xml"));

    //        SavedBackupsLoader.LoadInfos();
    //        SavedBackups.Clear();
    //        SavedBackupsLoader.GetInfos().ForEach(f =>
    //        {
    //            //f.TAG = new ProgBarsTag();
    //            SavedBackups.Add(f);
    //        });
    //    }


    //    private static void ForceLogin()
    //    {
    //        var login = new LoginWindow();
    //        if (login.ShowDialog() == false)
    //            Environment.Exit(0);
    //    }
    //}
}
