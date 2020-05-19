using SmartModulBackupClasses;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace smart_modul_BACKUP
{
    /// <summary>
    /// Interakční logika pro RestorePage.xaml
    /// </summary>
    public partial class RestorePage : Page
    {
        public Backup Backup { get; private set; }

        public List<SavedSourceSelected> Directories { get; private set; } = new List<SavedSourceSelected>();
        public List<SavedSourceSelected> Files { get; private set; } = new List<SavedSourceSelected>();
        public List<SavedSourceSelected> Databases { get; private set; } = new List<SavedSourceSelected>();

        public string localPath { get; set; }

        public RestorePage(Backup backupToRestore)
        {
            Backup = backupToRestore;
            localPath = Path.GetFullPath(Backup.LocalPath);

            //převést zdroje na SelectedSavedSource
            foreach (var src in Backup.Sources.Where(f => f.filename != null))
            {
                switch (src.type)
                {
                    case BackupSourceType.Database:
                        Databases.Add(_src(src));
                        break;
                    case BackupSourceType.Directory:
                        Directories.Add(_src(src));
                        break;
                    case BackupSourceType.File:
                        Files.Add(_src(src));
                        break;
                }
            }

            InitializeComponent();

            //skrýt panely, které nepotřebujem
            panel_foldersToRestore.Visibility = Directories.Any() ? Visibility.Visible : Visibility.Collapsed;
            panel_filesToRestore.Visibility = Files.Any() ? Visibility.Visible : Visibility.Collapsed;
            panel_dbsToRestore.Visibility = Databases.Any() ? Visibility.Visible : Visibility.Collapsed;

            //defaultně zaškrtnout možnost, kterou uživatel nejpravděpodobněji chce
            Backup.CheckLocalAvailibility();
            if (Backup.AvailableOnThisComputer)
                rbt_local.IsChecked = true;
            else if (Backup.AvailableRemotely)
                rbt_remote.IsChecked = true;
            else
            {
                rbt_local.IsChecked = true;
                localPath = "";
            }
        }

        private SavedSourceSelected _src(SavedSource src)
            => new SavedSourceSelected(src) { Selected = src.Success == BackupSuccessLevel.EverythingWorked };

        public Restore GetRestoreObject()
        {
            var restore = new Restore()
            {
                backupID = Backup.ID,
                location = rbt_local.IsChecked == true ? BackupLocation.Local : BackupLocation.SFTP,
                zip_path = rbt_local.IsChecked == true ? localPath : Backup.RemotePath
            };

            restore.sources = Utils.MultiUnion(Directories, Files, Databases)
                                .Where(f => f.Selected)
                                .Select(f => 
                                {
                                    if(f.OverrideSourcePath)
                                    {
                                        var toreturn = f.Value.Clone() as SavedSource;
                                        toreturn.sourcepath = f.RestorePath;
                                        return toreturn;
                                    }

                                    return f.Value;
                                })
                                .ToArray();

            return restore;
        }

        private void btn_click_back(object sender, RoutedEventArgs e)
        {
            MainWindow.main.Back();
        }

        private void btn_click_restore(object sender, RoutedEventArgs e)
        {
            LoadedStatic.service.Restore(GetRestoreObject());
            MainWindow.main.Back();
            
        }

        private void btn_click_cancel(object sender, RoutedEventArgs e)
        {
            MainWindow.main.Back();
        }

        private void dg_cancelSelection(object sender, SelectionChangedEventArgs e)
        {
            //(sender as DataGrid).UnselectAll();
        }
    }
}
