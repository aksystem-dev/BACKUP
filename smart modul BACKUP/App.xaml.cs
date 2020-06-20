using smart_modul_BACKUP.Managers;
using SmartModulBackupClasses;
using SmartModulBackupClasses.Managers;
using SmartModulBackupClasses.WebApi;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace smart_modul_BACKUP
{
    /// <summary>
    /// Interakční logika pro App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Argumenty aplikace. 
        /// </summary>
        public static string[] ARGS;

        /// <summary>
        /// Pokud je mezi argumenty "-force": 
        /// znamená to, že se má přeskočit logika, která vypne proces, pokud už na tomto uživateli stejný proces běží
        /// </summary>
        public static bool forceStart => ARGS.Contains("-force");

        /// <summary>
        /// Pokud je mezi argumenty "-autorun": 
        /// znamená to, že pokud služba není nainstalována nebo spuštěna,
        /// má se nainstalovat a spustit automaticky, aniž bychom se uživatele ptali, jestli si to tak přeje.
        /// </summary>
        public static bool autoRun => ARGS.Contains("-autorun");

        /// <summary>
        /// Pokud je mezi argumenty "-hidden":
        /// znamená to, že aplikace má začít skryta (čili schovaná v NotifyIcon)
        /// </summary>
        public static bool startHidden => ARGS.Contains("-hidden");

        //public static bool IsUserLoggedIn { get; set; } = false;

        private void OnAppStart(object sender, StartupEventArgs e)
        {
            ARGS = e.Args;

            //přemístit se do složky obsahující potřebné soubory
            string cd = "C:\\smart modul BACKUP";
            if (!Directory.Exists(cd)) Directory.CreateDirectory(cd);
            Directory.SetCurrentDirectory(cd);

            SMB_Log.OnLog += SMB_Log_OnLog;
            GuiLog.Clear();
            GuiLog.FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "guilog.txt");
            GuiLog.Log("\n===========\nGUI started\n===========\n");

            Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;

            try
            {
                Setup();
            }
            catch (UnauthorizedAccessException)
            {
                Utils.RestartAsAdmin(new string[] { });
            }
            catch (Exception ex)
            {
                GuiLog.Log($"==============================\nVýjimka! ({ex.GetType().Name})\n\n{ex.Message}\n\n{ex.StackTrace}\n==============================");

                var service = Manager.Get<ServiceState>();
                if (service != null && service.State == ServiceConnectionState.Connected)
                    service.Disconnect();

                Environment.Exit(1);
            }
        }

        private void Setup()
        {
            SetupGuicom();
            SetupConfig();
            var apiTask = Task.Run(SetupApiAsync);
            SetupNotifyIcon();
            SetupAvailableDbs();
            SetupSftp();
            Manager.SetSingleton(new InProgress());
            if (!apiTask.Result)
                ShowLogin(true);
            else
                Task.Run(SetupPlanManager).Wait();
            SetupService();
            SetupRules();
            SetupBackups();
        }

        private static void SetupSftp()
        {
            Manager.SetTransient(new SftpUploaderFactory());
        }

        private static void SetupBackups()
        {
            Manager.SetSingleton(new BackupInfoManager());
        }

        private static void SetupAvailableDbs()
        {
            Manager.SetSingleton(new AvailableDbLoader()).Load();
        }

        private static void SetupNotifyIcon()
        {
            //vytvořit notifyIcon
            var notifyIcon = new System.Windows.Forms.NotifyIcon();
            notifyIcon.Icon = smart_modul_BACKUP.Properties.Resources.ikona_smart_modul512;
            notifyIcon.Visible = true;
            notifyIcon.Text = "smart modul BACKUP";
            Manager.SetSingleton(notifyIcon);
        }

        private static void SetupGuicom()
        {
            var guicom = Manager.SetSingleton(new OneGuiPerUser());
            if (!App.forceStart)
            {
                if (!guicom.Init())
                    Environment.Exit(0);
            }

        }

        private static void SetupService()
        {
            Manager.SetSingleton(new ServiceState()).SetupWithMessageBoxes();
        }

        private static void SetupRules()
        {
            Manager.SetSingleton(new BackupRuleLoader()).Load();
        }

        private static void SetupConfig()
        {
            Manager.SetSingleton(new ConfigManager()).Load();
        }

        private void SMB_Log_OnLog(LogArgs obj)
        {
            GuiLog.Log($"#######################\n\n > {obj.Message} \n\n#######################");
        }

        private void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            GuiLog.Log($"==============================\nVýjimka! ({e.Exception.GetType().Name})\n\n{e.Exception.Message}\n\n{e.Exception.StackTrace}\n==============================");
        }

        private void OnAppExit(object sender, ExitEventArgs e)
        {
            Manager.Get<System.Windows.Forms.NotifyIcon>()?.Dispose();
        }

        /// <summary>
        /// Přidá SmbWebApi do Manageru
        /// </summary>
        /// <returns></returns>
        private async Task<bool> SetupApiAsync()
        {
            var config = Manager.Get<ConfigManager>().Config;

            SmbApiClient API = null;
            if (config.WebCfg == null)
            {
                return false;
            }
            else if (config.WebCfg.Offline)
            {

            }
            else
            {
                API = new SmbApiClient(config.WebCfg.Username, config.WebCfg.Password.Value, ms_timeout: 1500);

                try
                {
                    var hello = await API.HelloAsync().ConfigureAwait(false);
                    //IsUserLoggedIn = true;
                }
                catch (Exception ex)
                {
                    return false;
                }
            }

            Manager.SetSingleton(API);

            return true;
        }

        /// <summary>
        /// Přidá PlanManager
        /// </summary>
        /// <returns></returns>
        private async Task<bool> SetupPlanManager()
        {
            try
            {
                var plan_man = new PlanManager();
                await plan_man.LoadAsync();
                Manager.SetSingleton(plan_man);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Zobrazí okno Login; pokud dojde k úspěšnému přihlášení, nastaví SmbApiClient a PlanManager
        /// </summary>
        /// <returns></returns>
        public static bool ShowLogin(bool quitOnFail)
        {
            var login = new LoginWindow();
            var res = login.ShowDialog(); //login window by se mělo o vše postarat, proto můžeme vrátit
            if (res == true)
            {
                Manager.SetSingleton(login.api);
                Manager.SetSingleton(new PlanManager().Load());
                return true;
            }
            else
            {
                if (quitOnFail)
                    Environment.Exit(0);
                return false;
            }
        }

    }
}
