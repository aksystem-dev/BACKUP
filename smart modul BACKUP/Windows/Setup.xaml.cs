using SmartModulBackupClasses;
using SmartModulBackupClasses.Managers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace smart_modul_BACKUP.Windows
{
    /// <summary>
    /// Okno, které se otevře při prvním spuštění. Mělo by se spouštět s administrátorským oprávněním.
    /// </summary>
    public partial class Setup : Window
    {
        public ServiceState service { get; }

        public string LocalBkDir { get; set; }

        private readonly ConfigManager cfg_man;

        public Setup()
        {
            service = Manager.Get<ServiceState>();
            cfg_man = Manager.Get<ConfigManager>();
            LocalBkDir = cfg_man.Config.LocalBackupDirectory ?? Path.GetFullPath("Backups");

           // DialogResult = false;
            InitializeComponent();

            DoSetup();
        }

        async void DoSetup()
        {
            tabControl.SelectedIndex = 0;
            await Task.Run(() => service.SetupWithMessageBoxes(true, "smartModulBACKUP_service.exe"));

            if (service.State != ServiceConnectionState.Connected)
                Environment.Exit(0);

            tabControl.SelectedIndex = 1;
        }

        private void btn_setLocalBackups_click(object sender, RoutedEventArgs e)
        {
            try
            {
                Directory.CreateDirectory(LocalBkDir);
            }
            catch
            {
                MessageBox.Show("Cesta není validní.");
                return;
            }

            var cfg_man = Manager.Get<ConfigManager>();
            cfg_man.Config.LocalBackupDirectory = LocalBkDir;
            cfg_man.Config.FirstGuiRun = false;
            cfg_man.Save();

            try
            {
                service.Client.ReloadConfigAsync();
            }
            catch { }

            Close();
        }
    }
}
