using SmartModulBackupClasses;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
namespace smart_modul_BACKUP
{
    /// <summary>
    /// Interaction logic for RestoreResponseWindow.xaml
    /// </summary>
    public partial class RestoreResponseWindow : Window
    {
        public RestoreResponse Data { get; }

        public RestoreResponseWindow(RestoreResponse context)
        {
            Data = context;

            InitializeComponent();
        }

        private void btn_click_open(object sender, RoutedEventArgs e)
        {
            var el = sender as FrameworkElement;
            var src = el.DataContext as SavedSource;
            switch (src.type)
            {
                case BackupSourceType.Directory:
                    if (Directory.Exists(src.sourcepath))
                        Process.Start(Path.GetFullPath(src.sourcepath));
                    else
                        MessageBox.Show("Složka nenalezena.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
                case BackupSourceType.File:
                    var parent_dir = Path.GetDirectoryName(src.sourcepath);
                    if (Directory.Exists(parent_dir))
                        Process.Start(Path.GetFullPath(parent_dir));
                    else
                        MessageBox.Show("Složka nenalezena.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
            }
        }
    }
}
