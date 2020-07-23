using Microsoft.Win32;
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
using System.Reflection;
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

        public const string SERVICE_FNAME = "smartModulBACKUP_service.exe";

        /// <summary>
        /// Umožňuje volat funkce ve vlákně GUI z jiných vláken.
        /// </summary>
        /// <param name="a"></param>
        public static void dispatch(Action a)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    a();
                }
                catch (Exception ex)
                {
                    SmbLog.Error("Problém při Invokování delegáta", ex, LogCategory.GUI);
                }
            });
        }

        private void OnAppStart(object sender, StartupEventArgs e)
        {
            //Thread.Sleep(10000);
            ARGS = e.Args;

            //přemístit se do složky obsahující potřebné soubory
            //string cd = "C:\\smart modul BACKUP";
            //if (!Directory.Exists(cd)) Directory.CreateDirectory(cd);
            //Directory.SetCurrentDirectory(cd);

            //GUI pracuje ve složce, v níž se nachází jeho exe

            Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;

            try
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                
                Setup();
                stopwatch.Stop();
                SmbLog.Debug($"SETUP TIME: {stopwatch.Elapsed.ToString(@"hh\:mm\:ss\.fff")}", null, LogCategory.GuiSetup);
            }
            catch (UnauthorizedAccessException ex) when (!Utils.AmIAdmin()) //pokud nemáme dost oprávnění, řekneme si o administrátora
            {
                if (SmbLog.IsConfigured)
                    SmbLog.Info("Došlo k UnauthorizedAccessException, požaduji oprávnění administrátora", ex, LogCategory.GUI);
                Utils.RestartAsAdmin(new string[] { });
            }
            catch (Exception ex) //neošetřené výjimky vyblejt do logu a ukončit aplikaci
            {
                if (SmbLog.IsConfigured)
                    SmbLog.Fatal("Neošetřená výjimka při spouštění GUI", ex, LogCategory.GUI);

                //odpojení od služby
                var service = Manager.Get<ServiceState>();
                if (service != null && service.State == ServiceConnectionState.Connected)
                    service.Disconnect();

                Environment.Exit(1);
            }
        }

        private void Setup()
        {
            //vzhledem k tomu že je možné, že budeme otevírat nějaká okna ještě než se dostane na 
            //MainWindow, musíme říct aplikaci, aby se automaticky nezavírala
            //více info na https://stackoverflow.com/questions/3702785/wpf-application-exits-immediately-when-showing-a-dialog-before-startup
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            SetupGuicom(); //nastaví OneGuiPerUser

            //spustit ConfigManager
            var cfg_man = Manager.SetSingleton(new ConfigManager()).Load();
            SmbLog.Configure(cfg_man.Config.Logging, SmbAssembly.Gui);
            SmbLog.Info("Aplikace spuštěna, načtena konfigurace pro logování");

            var service = Manager.SetSingleton(new ServiceState());
            //service.Setup();

            //pokud je toto první spuštění aplikace
            if (cfg_man.Config.FirstGuiRun)
            {
                //potřebujeme administrátorská oprávnění
                if (!Utils.AmIAdmin())
                    Utils.RestartAsAdmin(new string[] { });

                //otevřeme "průvodce instalací"
                var setup_window = new Windows.Setup();
                setup_window.ShowDialog();
            }
            else
                //pokud jsme aplikaci již v minulosti spustili, použijeme standardní
                //metodu SetupWithMessageBoxes pro připojení ke službě.
                service.SetupWithMessageBoxes(autoRun, App.SERVICE_FNAME);

            var apiTask = Task.Run(SetupApiAsync);

            try
            {
                Utils.SetAutoRun(); //nastavit, aby se GUI automaticky spouštělo po přihlášení
            }
            catch { }

            SetupNotifyIcon(); //vytvořit NotifyIcon
            SetupAvailableDbs(); //nastavit AvailableDbLoader
            SetupSftp(); //nastavit SftpUploaderFactory
            Manager.SetSingleton(new InProgress()); //nastavit InProgress

            apiTask.Wait(); //počkat na task
            
            SetupRules(); //nastavit BackupRuleLoader
            SetupBackups(); //nastavit BackupInfoManager

            //po vyhodnocení metody Setup (v níž se mohou otevírat dialogová okna) můžeme
            //zase nastavit ShutdownMode tak, aby se aplikace vypla pokud se vypne MainWindow
            ShutdownMode = ShutdownMode.OnMainWindowClose;
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

        /// <summary>
        /// Nastaví, aby se tento program automaticky spouštěl po přihlášení uživatele do PC
        /// (upravením registru)
        /// </summary>


        /// <summary>
        /// Přidá SmbWebApi do Manageru
        /// </summary>
        /// <returns>True, pokud všemu rozumíme, false, pokud nějaké info chybí a je proto třeba otevřít přihlašovací okno</returns>
        private async Task<bool> SetupApiAsync()
        {
            var account = Manager.SetSingleton(new AccountManager());

            //neboť na PropertyChanged AccountManageru budeme věšet UI funkce a není vyloučeno, 
            //že se bude volat z jiného vlákna, nastavíme ho na dispatch metodu
            account.propertyChangedDispatcher = dispatch; 

            var config = Manager.Get<ConfigManager>().Config;

            return await account.TryLoginWithAsync(config.WebCfg);

            //if (config.WebCfg == null)
            //{
            //    //pokud nemáme info o připojení k API, pravděpodobně se jedná o první spuštění aplikace
            //    //vrátíme proto false, čímž volající metodě sdělíme, že chceme otevřít dialogové okno pro přihlášení
            //    return await account.TryLoginWithAsync(config.WebCfg);
            //}
            //else
            //{
            //    //TryLoginWithAsync vrátí true, pokud buďto:
            //    //   a) používáme aplikaci offline
            //    //   b) používáme aplikaci online a úspěšně jsme se připojili na api
            //    //ale vrátí false, pokud se něco nezdaří
            //    //pokud se tedy něco nezdaří, vrátíme false, čímž se otevře login okno, jinak vrátíme true, čímž 
            //    //se nestane nic
            //    return await account.TryLoginWithAsync(config.WebCfg);
            //}
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
        private static void SetupAvailableDbs()
        {
            var adl = Manager.SetSingleton(new AvailableDbLoader());
            Task.Run(adl.Load);
        }
        private static void SetupSftp()
        {
            Manager.SetTransient(new SftpUploaderFactory());
        }
        private static void SetupRules()
        {
            var rloader = Manager.SetSingleton(new BackupRuleLoader()).Load();
            rloader.UI_Dispatcher = dispatch;
            rloader.OnRuleUpdated += Rloader_OnRuleUpdated;
        }
        private static void SetupBackups()
        {
            var man = Manager.SetSingleton(new BackupInfoManager());

            //aby mohlo GUI odpovídat na PropertyChanged událost, musí se nastavit invokující delegát
            man.PropertyChangedDispatchHandler = dispatch;

            //do GUI chceme načítat pouze informace o zálohách, které jsou dostupné
            man.DefaultFilter = bk => bk.AvailableOnCurrentSftpServer || bk.AvailableOnThisComputer;

            _ = man.LoadAsync();
        }


        /// <summary>
        /// Když v GUI upravíme / přidáme pravidlo, informujem o tom službu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Rloader_OnRuleUpdated(object sender, BackupRule e)
        {
            try
            {
                Manager.Get<ServiceState>().Client.SetRule(e.ToXmlString());
            }
            catch { }
        }


        private void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            SmbLog.Fatal("Neošetřená výjimka v GUI", e.Exception, LogCategory.GUI);
        }

        private void OnAppExit(object sender, ExitEventArgs e)
        {
            Manager.Get<System.Windows.Forms.NotifyIcon>()?.Dispose();
        }

        /// <summary>
        /// odhlásí klienta z webu
        /// </summary>
        public static void Logout()
        {
            //updatovat konfiguraci
            var cfg_man = Manager.Get<ConfigManager>();
            cfg_man.Config.WebCfg.Online = false;
            cfg_man.Save();

            var _account = Manager.Get<AccountManager>();
            _ = _account.Api.DeactivateAsync();

            try
            {
                _account.TryLoginWithAsync(cfg_man.Config.WebCfg).Wait();
            }
            catch (Exception ex)
            {
                SmbLog.Error("Chyba při odhlašování GUI", ex, LogCategory.WebApi);
            }
            
            try
            {
                var service = Manager.Get<ServiceState>();
                if (service.State == ServiceConnectionState.Connected)
                    service.Client.UpdateApiAsync(); //říct službě, ať se také znovu připojí k api
            }
            catch (Exception ex)
            {
                SmbLog.Error("Chyba při volání UpdateApiAsync na službě", ex, LogCategory.GuiServiceClient);
            }
        }

        /// <summary>
        /// Zobrazí okno Login; pokud dojde k úspěšnému přihlášení, nastaví AccountManager
        /// </summary>
        /// <returns></returns>
        public static bool ShowLogin(bool quitOnFail)
        {
            var login = new LoginWindow();
            var res = login.ShowDialog(); //login window by se mělo o vše postarat
            if (res == true)
            {
                try
                {
                    var service = Manager.Get<ServiceState>();
                    if (service.State == ServiceConnectionState.Connected)
                        service.Client.UpdateApiAsync(); //říct službě, ať se také znovu připojí k api
                }
                catch (Exception ex)
                {
                    SmbLog.Error("Chyba při volání UpdateApiAsync na službě", ex, LogCategory.GuiServiceClient);
                }
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
