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
        public ConfigPage()
        {
            cfg_man = Manager.Get<ConfigManager>();

            InitializeComponent();
            DataContext = cfg_man.Config;
            LoadConfigToPasswords();
            Loaded += ConfigPage_Loaded;
        }

        private void ConfigPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadConfigToPasswords();   
        }

        public void LoadConfigToPasswords()
        {
            PasswordSFTP.SetPassword(cfg_man.Config.SFTP.Password.Value);
            PasswordSQL.SetPassword(cfg_man.Config.Connection.Password.Value);
        }

        public void UpdateConfig()
        {
            cfg_man.Config.SFTP.Password.Value = PasswordSFTP.GetPassword();
            cfg_man.Config.Connection.Password.Value = PasswordSQL.GetPassword();
        }

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

        private void page_unloaded(object sender, RoutedEventArgs e)
        {
            UpdateConfig();
            if (cfg_man.Config.UnsavedChanges)
                cfg_man.Save();
        }
    }
}
