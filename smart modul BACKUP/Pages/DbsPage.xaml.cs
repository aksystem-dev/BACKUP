using smart_modul_BACKUP.Managers;
using SmartModulBackupClasses;
using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace smart_modul_BACKUP
{
    /// <summary>
    /// Interakční logika pro DbsPage.xaml
    /// </summary>
    public partial class DbsPage : Page
    {
        public DbsPage()
        {
            InitializeComponent();

            //budou se zde zobrazovat dostupné databáze
            DataContext = Manager.Get<AvailableDbLoader>().availableDatabases;
        }


        private void BackupChecked(object sender, RoutedEventArgs e)
        {
            var db = ((sender as FrameworkElement).DataContext as Database);
            SetDbIncluded(db, true);
        }

        private void BackupUnchecked(object sender, RoutedEventArgs e)
        {
            var db = ((sender as FrameworkElement).DataContext as Database);
            SetDbIncluded(db, false);
        }

        private void SetDbIncluded(Database db, bool included)
        {
            db.IsNew = false;
            db.Include = included;
        }

        private void BackupCheckboxLoaded(object sender, RoutedEventArgs e)
        {
            var radio = (sender as RadioButton);
            var db = (radio.DataContext as Database);
            radio.IsChecked = !db.IsNew && db.Include;
        }

        private void BackupUncheckboxLoaded(object sender, RoutedEventArgs e)
        {
            var radio = (sender as RadioButton);
            var db = (radio.DataContext as Database);
            radio.IsChecked = !db.IsNew && !db.Include;
        }
    }
}
