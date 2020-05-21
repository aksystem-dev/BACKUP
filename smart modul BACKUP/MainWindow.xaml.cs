using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Diagnostics;
using SmartModulBackupClasses;

namespace smart_modul_BACKUP
{
    /// <summary>
    /// Interakční logika pro MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Dictionary<ButtonWithState, Page> buttons = new Dictionary<ButtonWithState, Page>();

        public HomePage homePage { get; private set; }
        public RulePage rulePage { get; private set; }
        public ConfigPage configPage { get; private set; }
        public BackupsPage backupsPage { get; private set; }

        public object lastPage { get; private set; } = null;

        //ikona aplikace, která bude zobrazovat bubliny
        private System.Windows.Forms.NotifyIcon notifyIcon = new System.Windows.Forms.NotifyIcon();

        private OneGuiPerUser guicom;

        private RuleIdController ticket_please_guy;

        public static MainWindow main;

        public MainWindow()
        {
            main = this;

            try
            {
                //přemístit se do složky obsahující potřebné soubory
                string cd = "C:\\smart modul BACKUP";
                if (!Directory.Exists(cd)) Directory.CreateDirectory(cd);
                Directory.SetCurrentDirectory(cd);

                SMB_Log.OnLog += SMB_Log_OnLog;
                GuiLog.Clear();
                GuiLog.FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "guilog.txt");
                GuiLog.Log("\n===========\nGUI started\n===========\n");

                Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;

                guicom = new OneGuiPerUser();
                if (!App.forceStart)
                {
                    if (!guicom.Init())
                        Environment.Exit(0);
                    else
                        guicom.OpenSignalReceived += ShowMyself;
                }

                //

                //načíst pravidla a config.xml
                LoadedStatic.Load();
                LoadedStatic.service.SetupWithMessageBoxes(App.autoRun);

                InitializeComponent();

                //vytvořit stránky
                homePage = new HomePage();
                rulePage = new RulePage();
                configPage = new ConfigPage();
                backupsPage = new BackupsPage();

                //nastavit tlačítka na navigaci
                buttons.Add(btn_home, homePage);
                buttons.Add(btn_rules, rulePage);
                //buttons.Add(btn_dbs, new Uri("Pages\\DbsPage.xaml", UriKind.Relative));
                buttons.Add(btn_config, configPage);
                buttons.Add(btn_backups, backupsPage);

                //až se okno zavře, uložit konfiguraci a pravidla
                Closed += MainWindow_Closed;

                //až budeme ukládat, musíme nejdřív uložit hesla (PasswordBoxy totiž nepodporují binding)
                LoadedStatic.beforeSave += () =>
                {
                    if (frame.Content is ConfigPage cfgPage)
                        cfgPage.UpdateConfig();
                };

                //po uložení chceme aktualizovat službu
                //LoadedStatic.afterSave += LoadedStatic.service.Reload;

                //vytvořit notifyIcon
                notifyIcon.Icon = Properties.Resources.ikona_smart_modul512;
                notifyIcon.Visible = true;
                notifyIcon.Text = "smart modul BACKUP";
                notifyIcon.Click += (_, __) => ShowMyself();

                LoadedStatic.notifyIcon = notifyIcon;

                //vytvořit instanci RuleIdController a zařídit, aby kontrolovala všechna nově přidaná pravidla
                ticket_please_guy = new RuleIdController("ruleid");
                ticket_please_guy.Init(LoadedStatic.rules);
                LoadedStatic.rules.CollectionChanged += Rules_CollectionChanged;

                //skrýt apku, má-li začít skryta
                if (App.startHidden)
                    Hide();
            }
            catch (UnauthorizedAccessException)
            {
                Utils.RestartAsAdmin(new string[] {  });
            }
            catch (Exception e)
            {
                GuiLog.Log($"==============================\nVýjimka! ({e.GetType().Name})\n\n{e.Message}\n\n{e.StackTrace}\n==============================");

                if (LoadedStatic.service.State == ServiceConnectionState.Connected)
                    LoadedStatic.service.Disconnect();

                Environment.Exit(1);
            }
        }

        private void SMB_Log_OnLog(LogArgs obj)
        {
            GuiLog.Log($"#######################\n\n > {obj.Message} \n\n#######################");
        }

        private void Rules_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                foreach (var obj in e.NewItems)
                {
                    var rule = obj as BackupRule;

                    //když je přidáno pravidlo, chceme, aby ho ticket_please_guy zkontroloval
                    ticket_please_guy.TicketsPlease(rule);

                    //také nastavit nějaké defaultní hodnoty
                    rule.LocalBackups.MaxBackups = 5;
                    rule.RemoteBackups.MaxBackups = 50;
                }
            }
        }

        private void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            GuiLog.Log($"==============================\nVýjimka! ({e.Exception.GetType().Name})\n\n{e.Exception.Message}\n\n{e.Exception.StackTrace}\n==============================");
        }

        private void ShowMyself()
        {
            Dispatcher.Invoke(() =>
            {
                Show();
                Activate();
            });
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            //notifyIcon.Dispose();
            LoadedStatic.service.Disconnect();
        }

        //přenaviguje frame na danou stránku a zařídí, aby příslušné tlačítko změnilo barvu (btn.On = true)
        private void nav(object sender, RoutedEventArgs e)
        {
            foreach (var btn in buttons.Keys)
            {
                if (btn == sender)
                    btn.On = true;
                else
                    btn.On = false;
            }

            var page = buttons[sender as ButtonWithState];
            if (page != null)
            {
                //pokud odcházíme z config stránky, musíme uložit hesla do config objektu
                //   (to je tím, že nějaký debil v microsoftu se rozhodl, že pokud se z té stránky odejde, passwordboxy se vyprázdní)
                if (frame.Content is ConfigPage cfgPage)
                    cfgPage.UpdateConfig();

                frame.Navigate(page);
            }
        }

        private bool cancelClose = true;
        private void window_closing(object sender, CancelEventArgs e)
        {
            //uložit konfiguraci
            LoadedStatic.SaveAll();
            LoadedStatic.service.Reload();

            if (cancelClose)
            {
                //aplikace se nevypne, pouze se skryje
                e.Cancel = true;
                Hide();
            }          
        }

        private void closeWindow(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void shutdown(object sender, RoutedEventArgs e)
        {
            if (System.Windows.MessageBox.Show("Vypnutím GUI nebudete dostávat oznámení o stavu služby. Jste si jisti?",
                "Otázka",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question)
                    == MessageBoxResult.No)
                return;

            cancelClose = false;
            Close();
        }

        private void RetryConnection(object sender, RoutedEventArgs e)
        {
            if (LoadedStatic.service.State == ServiceConnectionState.Connected)
                LoadedStatic.service.Disconnect();

            var state = LoadedStatic.service.State;
            LoadedStatic.service.SetupWithMessageBoxes(
                state == ServiceConnectionState.NotInstalled || state == ServiceConnectionState.NotRunning);
        }

        private void AllNavButtonsSetChecked(bool value)
        {
            foreach(var btn in buttons.Keys)
                btn.On = value;
        }

        public void ShowPage(Page page)
        {
            frame.Navigate(page);

            if (buttons.ContainsValue(page))
            {
                foreach (var btn in buttons.Keys)
                {
                    if (buttons[btn] == page)
                        btn.On = true;
                    else
                        btn.On = false;
                }

                return;
            }

            AllNavButtonsSetChecked(false);
        }

        private void frame_navigating(object sender, System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            lastPage = frame.Content;
        }

        public void Back()
        {
            if (lastPage != null)
                frame.Content = lastPage;
        }
    }


}
