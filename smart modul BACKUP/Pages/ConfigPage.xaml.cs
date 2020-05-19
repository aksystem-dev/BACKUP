using Renci.SshNet;
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
        public ConfigPage()
        {
            InitializeComponent();
            DataContext = LoadedStatic.config;
            LoadConfigToPasswords();
            Loaded += ConfigPage_Loaded;
        }

        private void ConfigPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadConfigToPasswords();   
        }

        public void LoadConfigToPasswords()
        {
            PasswordSFTP.SetPassword(LoadedStatic.config.SFTP.Password);
            PasswordSQL.SetPassword(LoadedStatic.config.Connection.Password);
        }

        public void UpdateConfig()
        {
            LoadedStatic.config.SFTP.Password = PasswordSFTP.GetPassword();
            LoadedStatic.config.Connection.Password = PasswordSQL.GetPassword();
        }

        private void TestSQL(object sender, RoutedEventArgs e)
        {
            UpdateConfig();

            Task.Run(() =>
            {
                SqlConnection conn = new SqlConnection(LoadedStatic.config.Connection.GetConnectionString(1));
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
        }

        private void TestSFTP(object sender, RoutedEventArgs args)
        {
            UpdateConfig();
            
            var c = LoadedStatic.config.SFTP;

            Task.Run(() =>
            {
                var client = new SftpClient(c.Adress, c.Port, c.Username, c.Password);

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
        }
    }
}
