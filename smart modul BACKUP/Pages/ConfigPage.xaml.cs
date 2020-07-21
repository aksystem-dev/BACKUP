using Renci.SshNet;
using SmartModulBackupClasses;
using SmartModulBackupClasses.Managers;
using System;
using System.Collections.Generic;
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
    public partial class ConfigPage : Page
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
            LoadConfigToPasswords();
            Loaded += ConfigPage_Loaded;
        }

        private void ConfigPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadConfigToPasswords();   
        }

        /// <summary>
        /// Nastavit obsah PasswordBoxů na hesla v konfiguraci
        /// </summary>
        public void LoadConfigToPasswords()
        {
            PasswordSFTP.SetPassword(cfg_man.Config.SFTP.Password.Value);
            PasswordSQL.SetPassword(cfg_man.Config.Connection.Password.Value);
        }

        /// <summary>
        /// Nastavit hesla v konfiguraci podle PasswordBoxů
        /// </summary>
        public void UpdateConfig()
        {
            if (IsLoaded)
            {
                cfg_man.Config.SFTP.Password.Value = PasswordSFTP.GetPassword();
                cfg_man.Config.Connection.Password.Value = PasswordSQL.GetPassword();
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

        // Ukládání konfigurace je pořešeno v MainWindow.xaml.cs, metoda saveCfg
        private void page_unloaded(object sender, RoutedEventArgs e)
        {
            //UpdateConfig();
            //if (cfg_man.Config.UnsavedChanges)
            //    cfg_man.Save();
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
                await Manager.Get<ServiceState>().Client.CleanupBackupsAsync();
            }
            catch { }
        }

        /// <summary>
        /// Vypnout službu, pokud uživatel odpoví ano na MessageBox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_click_turnServiceOff(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(
                "Když je služba vypnutá, apikace smart modul BACKUP nemůže vytvářet zálohy. Opravdu ji chcete vypnout?",
                "Otázka", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                if (service.StopService())
                    MessageBox.Show("Služba byla vypnuta.");
                else
                    MessageBox.Show("Službu se nepodařilo vypnout.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
