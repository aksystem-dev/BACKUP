using Renci.SshNet;
using SmartModulBackupClasses;
using SmartModulBackupClasses.Mails;
using SmartModulBackupClasses.Managers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace smart_modul_BACKUP
{
    /// <summary>
    /// Interakční logika pro ConfigPage.xaml
    /// </summary>
    public partial class ConfigPage : Page, INotifyPropertyChanged
    {
        ConfigManager cfg_man;

        public ServiceState service { get; set; }
        public AccountManager Plan_Man { get; set; }
        public ConfigPage()
        {
            cfg_man = Manager.Get<ConfigManager>();
            Plan_Man = Manager.Get<AccountManager>();
            service = Manager.Get<ServiceState>();

            InitializeComponent();
            DataContext = cfg_man.Config;
            LoadConfig();
            Loaded += ConfigPage_Loaded;
        }

        private void ConfigPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadConfig();


            if (Plan_Man.State == LoginState.Offline)
            {
                btn_login.Visibility = Visibility.Visible;
                btn_logout.Visibility = Visibility.Collapsed;
            }
            else
            {
                btn_login.Visibility = Visibility.Collapsed;
                btn_logout.Visibility = Visibility.Visible;
            }
        }

        //ukládání konfigurace při odnatčení stránky pořešeno v MainWindow.xaml.cs
        private void page_unloaded(object sender, RoutedEventArgs e)
        {
        }

        /// <summary>
        /// Nastavit obsah PasswordBoxů na hesla v konfiguraci, a další funkce
        /// </summary>
        public void LoadConfig()
        {
            PasswordSFTP.SetPassword(cfg_man.Config.SFTP.Password.Value);
            PasswordSQL.SetPassword(cfg_man.Config.Connection.Password.Value);
            PasswordSMTP.SetPassword(cfg_man.Config.EmailConfig.Password.Value);

            ToAddresses.Clear();
            cfg_man.Config.EmailConfig.ToAddresses.ForEach(str => ToAddresses.Add(new Models.ObservableString(str)));
        }

        /// <summary>
        /// Nastavit hesla v konfiguraci podle PasswordBoxů a další funkce
        /// </summary>
        public void UpdateConfig()
        {
            if (IsLoaded)
            {
                cfg_man.Config.SFTP.Password.Value = PasswordSFTP.GetPassword();
                cfg_man.Config.Connection.Password.Value = PasswordSQL.GetPassword();
                cfg_man.Config.EmailConfig.Password.Value = PasswordSMTP.GetPassword();

                cfg_man.Config.EmailConfig.ToAddresses.Clear();
                cfg_man.Config.EmailConfig.ToAddresses.AddRange(ToAddresses.Select(str => str.Value));
            }
        }

        /// <summary>
        /// Otestovat, zdali jsou zadané údaje na připojení na SQL server platné
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void TestSQL(object sender, RoutedEventArgs e)
        {
            UpdateConfig();

            btn_testsql.IsEnabled = false;

            await Task.Run(() =>
            {
                SqlConnection conn = new SqlConnection(cfg_man.Config.Connection.GetConnectionString(1));
                try
                {
                    conn.Open();
                    conn.Close();
                    MessageBox.Show("Připojení bylo úspěšné", "Test SQL připojení", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch
                {
                    MessageBox.Show("Připojení se nezdařilo", "Test SQL připojení", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            });

            btn_testsql.IsEnabled = true;
        }

        /// <summary>
        /// Otestovat, zdali jsou zadané údaje na připojení na SFTP platné
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async void TestSFTP(object sender, RoutedEventArgs args)
        {
            UpdateConfig();
            
            var c = cfg_man.Config.SFTP;

            btn_testsftp.IsEnabled = false;

            await Task.Run(() =>
            {
                var client = new SftpClient(c.Host, c.Port, c.Username, c.Password.Value);

                try
                {
                    client.Connect();
                    client.Disconnect();
                    MessageBox.Show("Připojení bylo úspěšné", "Test SFTP připojení", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception e)
                {
                    MessageBox.Show($"Připojení se nezdařilo ({e.GetType().Name})\n\n{e.Message}", "Test SFTP připojení", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            });

            btn_testsftp.IsEnabled = true;
        }

        /// <summary>
        /// Předat službě požadavek na pročištění záloh (použitím BackupCleaner).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void click_cleanup_backups(object sender, RoutedEventArgs e)
        {
            try
            {
                await Task.Run(Manager.Get<ServiceState>().Client.CleanupBackups);
                MessageBox.Show("Zálohy byly pročištěny.");
            }
            catch { }
        }

        /// <summary>
        /// Vypnout službu, pokud uživatel odpoví ano na MessageBox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btn_click_turnServiceOff(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(
                "Když je služba vypnutá, apikace smart modul BACKUP nemůže vytvářet zálohy. Opravdu ji chcete vypnout?",
                "Otázka", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                if (await service.StopService())
                    MessageBox.Show("Služba byla vypnuta.");
                else
                    MessageBox.Show("Službu se nepodařilo vypnout.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private async void RetryConnection(object sender, RoutedEventArgs e)
        {
            var service = Manager.Get<ServiceState>();
            if (service == null)
                MessageBox.Show("Došlo k nějaké podivné chybě, příteli");

            await Task.Run(() =>
            {
                if (service.State == ServiceConnectionState.Connected)
                    service.Disconnect();

                var state = service.State;
                service.SetupWithMessageBoxes(
                    state == ServiceConnectionState.NotInstalled || state == ServiceConnectionState.NotRunning,
                    App.SERVICE_FNAME);
            });
        }


        /// <summary>
        /// Většina věcí se binduje přímo na Config, ale na seznam e-mailových adres se udržuje vlastní
        /// ObservableCollection, které se teprve po zavření aplikuje na EmailConfig.ToAdresses
        /// </summary>
        public ObservableCollection<Models.ObservableString> ToAddresses { get; set; }
            = new ObservableCollection<Models.ObservableString>();

        private void click_add_emailReceiver(object sender, RoutedEventArgs e)
        {
            ToAddresses.Add(new Models.ObservableString(""));
        }

        private void click_remove_emailReceiver(object sender, RoutedEventArgs e)
        {
            foreach (var i in checkedEmailIndexes)
                ToAddresses.RemoveAt(i);

            checkedEmailIndexes.Clear();

            
        }

        HashSet<int> checkedEmailIndexes = new HashSet<int>();

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Přidá index checkboxu v itemscontrolu na seznam checkedEmailIndexes. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void on_email_checked(object sender, RoutedEventArgs e)
        {
            var element = sender as FrameworkElement;
            for (int i = 0; i < ic_emails_toAdresses.Items.Count; i++)
            {
                if (element.IsDescendantOf(ic_emails_toAdresses.ItemContainerGenerator.ContainerFromIndex(i)))
                {
                    checkedEmailIndexes.Add(i);
                    return;
                }
            }
        }

        /// <summary>
        /// Odstraní index checkboxu v itemscontrolu ze seznamu checkedEmailIndexes. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void on_email_unchecked(object sender, RoutedEventArgs e)
        {
            var element = sender as FrameworkElement;
            for (int i = 0; i < ic_emails_toAdresses.Items.Count; i++)
            {
                if (element.IsDescendantOf(ic_emails_toAdresses.ItemContainerGenerator.ContainerFromIndex(i)))
                {
                    checkedEmailIndexes.Remove(i);
                    return;
                }
            }
        }

        //když klikneme na "přihlásit", 
        private void click_login(object sender, RoutedEventArgs e)
        {
            App.ShowLogin(false); //zobrazit login okno

            //načíst informace, které se změnou stavu přihlášení mohly změnit
            Task.Run(async () =>
            {
                Manager.Get<BackupRuleLoader>().Load();
                await Manager.Get<BackupInfoManager>().LoadAsync();
            });
        }

        //když klikneme na "odhlásit", 
        private void click_logout(object sender, RoutedEventArgs e)
        {
            App.Logout();
        }

        private bool _testingSmtp = false;
        public bool TestingSmtp
        {
            get => _testingSmtp;
            set
            {
                _testingSmtp = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TestingSmtp)));
            }
        }

        private async void TestSMTP(object sender, RoutedEventArgs e)
        {
            if (TestingSmtp)
                return;

            UpdateConfig();

            TestingSmtp = true;

            var config = cfg_man.Config.EmailConfig.Copy();
            config.ToAddresses.Clear();
            config.ToAddresses.AddRange(ToAddresses.Select(str => str.Value));

            var mailer = new Mailer();

            var result = await mailer.SendDumbEachAsync(new Mail()
            {
                Content = "Toto je testovací mail (smart modul BACKUP)",
                Html = false,
                Subject = "Testovací mail"
            }, cfg: config);

            if (result.Success)
                MessageBox.Show("Odeslání mailu bylo úspěšné!", "Test", MessageBoxButton.OK, MessageBoxImage.Information);
            else
                MessageBox.Show($"Odeslání mailu se nezdařilo.", "Test", MessageBoxButton.OK, MessageBoxImage.Error);

            TestingSmtp = false;
        }

        private async void SyncSFTP(object sender, RoutedEventArgs e)
        {
            btn_syncsftp.IsEnabled = false;
            UpdateConfig();
            try
            {
                //vytvořit a otevřít okno, kde si uživatel vybere, z kterých klientů chce stáhnout data
                var win = new Windows.SftpSyncSelectWindow(SftpMetadataManager.GetPCInfos());
                if (win.ShowDialog() == false)
                    return;

                //vytvořit hashset s vybranými počítači
                var selectedPCs = win.SelectedPCs.ToHashSet();

                var man = Manager.Get<BackupInfoManager>();
                var opt = man.DefaultOptions.With(options =>
                {
                    options.DownloadSFTP = true;
                    options.SftpClientFilter = selectedPCs.Contains;
                });
                await man.LoadAsync(opt);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Nepovedlo se to. \n {ex.Message}");
            }
            finally
            {
                btn_syncsftp.IsEnabled = true;
            }
        }
    }
}
