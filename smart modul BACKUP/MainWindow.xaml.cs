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
using SmartModulBackupClasses.WebApi;
using SmartModulBackupClasses.Managers;

namespace smart_modul_BACKUP
{
    /// <summary>
    /// Interakční logika pro MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private Dictionary<ButtonWithState, Page> buttons = new Dictionary<ButtonWithState, Page>();

        public HomePage homePage { get; private set; }
        public RulePage rulePage { get; private set; }
        public ConfigPage configPage { get; private set; }
        public BackupsPage backupsPage { get; private set; }
        public AboutPage aboutPage { get; private set; }

        public object lastPage { get; private set; } = null;

        public static MainWindow main;

        public ServiceState service { get; set; }

        public SmbApiClient client { get; set; }

        public MainWindow()
        {
            main = this;
            service = Manager.Get<ServiceState>();
            client = Manager.Get<SmbApiClient>();
            Manager.OnImplementationSet += Manager_OnImplementationSet;

            InitializeComponent();

            //vytvořit stránky
            homePage = new HomePage();
            rulePage = new RulePage();
            configPage = new ConfigPage();
            backupsPage = new BackupsPage();
            aboutPage = new AboutPage();

            //nastavit tlačítka na navigaci
            buttons.Add(btn_home, homePage);
            buttons.Add(btn_rules, rulePage);
            //buttons.Add(btn_dbs, new Uri("Pages\\DbsPage.xaml", UriKind.Relative));
            buttons.Add(btn_config, configPage);
            buttons.Add(btn_backups, backupsPage);
            buttons.Add(btn_about, aboutPage);

            //až se okno zavře, uložit konfiguraci a pravidla
            Closed += MainWindow_Closed;

            Manager.Get<System.Windows.Forms.NotifyIcon>().Click += (_, __) => ShowMyself();
            Manager.Get<OneGuiPerUser>().OpenSignalReceived += ShowMyself;

            //skrýt apku, má-li začít skryta
            if (App.startHidden)
                Hide();
        }

        //pokud by se náhodou z nějakého důvodu zavolalo podruhé Manager.SetSingleton<SmbApiClient>,
        //chceme to mít porychtovaný
        private void Manager_OnImplementationSet(ImplementationSetEventArgs obj)
        {
            if (obj.Type == typeof(SmbApiClient) && obj.Singleton)
            {
                client = obj.NewImplementation as SmbApiClient;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(client)));
            }
        }

        private void ShowMyself()
        {
            App.dispatch(() =>
            {
                Show();
                Activate();
            });
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            //notifyIcon.Dispose();
            Manager.Get<ServiceState>()?.Disconnect();
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
                //pokud odnačítáme ConfigPage, musíme uložit hesla a konfiguraci
                if (frame.Content is ConfigPage)
                    saveCfg();

                frame.Navigate(page);
            }
        }

        ///// <summary>
        ///// uložit konfig
        ///// </summary>
        public void saveCfg()
        {
            configPage.UpdateConfig();
            Manager.Get<ConfigManager>().Save();

            try
            {
                Manager.Get<ServiceState>().Client.ReloadConfigAsync();
            }
            catch { }
        }

        private bool cancelClose = true;

        public event PropertyChangedEventHandler PropertyChanged;

        private void window_closing(object sender, CancelEventArgs e)
        {
            var trace = new StackTrace();

            //uložit konfiguraci, pokud je otevřená ConfigPage
            if (frame.Content is ConfigPage)
                saveCfg();

            Manager.Get<ServiceState>().Reload();

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
