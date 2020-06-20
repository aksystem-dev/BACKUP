using SmartModulBackupClasses;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace smart_modul_BACKUP
{
    /// <summary>
    /// Interakční logika pro RestoreWindow.xaml
    /// </summary>
    public partial class RestoreWindow : Window
    {
        public string localPath { get; set; }

        public Restore restoreInfo { get; private set; } = null;

        public Backup backupInfo { get; private set; }

        public ObservableCollection<SavedSourceSelected> backupSourcesDatabases { get; private set; }
            = new ObservableCollection<SavedSourceSelected>();

        public ObservableCollection<SavedSourceSelected> backupSourcesFiles { get; private set; }
            = new ObservableCollection<SavedSourceSelected>();

        public ObservableCollection<SavedSourceSelected> backupSourcesDirectories { get; private set; }
            = new ObservableCollection<SavedSourceSelected>();

        public RestoreWindow(Backup backup)
        {
            backupInfo = backup;
            foreach (var i in from src in backup.Sources select new SavedSourceSelected(src) { Selected = true })
            {
                switch (i.Value.type)
                {
                    case BackupSourceType.Database:
                        backupSourcesDatabases.Add(i);
                        break;
                    case BackupSourceType.Directory:
                        backupSourcesDirectories.Add(i);
                        break;
                    case BackupSourceType.File:
                        backupSourcesFiles.Add(i);
                        break;
                }
            }

            localPath = backupInfo.LocalPath;

            InitializeComponent();

            if (backupInfo.AvailableRemotely) rbt_remote.IsChecked = true;
            else rbt_local.IsChecked = true;
        }

        private void ok(object sender, RoutedEventArgs e)
        {
            restoreInfo = new Restore()
            {
                backupID = backupInfo.LocalID,
                location = rbt_local.IsChecked == true ? BackupLocation.Local : BackupLocation.SFTP,
                zip_path = rbt_local.IsChecked == true ? localPath : backupInfo.RemotePath,
                sources = Utils.MultiUnion<SavedSourceSelected>(backupSourcesDatabases, backupSourcesDirectories, backupSourcesFiles)
                            .Where(f => f.Selected).Select(f => f.Value).ToArray()
            };

            //LoadedStatic.service.client.Restore(restoreInfo);
            Manager.Get<ServiceState>()?.StartRestore(restoreInfo);
            Close();
        }

        private void cancel(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
