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
    /// Interakční logika pro SingleRulePage.xaml
    /// </summary>
    public partial class SingleRulePage : Page
    {
        public SingleRulePage(BackupRule rule)
        {
            DataContext = rule;

            InitializeComponent();
        }

        private void btn_click_back(object sender, RoutedEventArgs e)
        {
            MainWindow.main.Back();
        }

        private void btn_click_backup(object sender, RoutedEventArgs e)
        {
            BackupRule Rule = DataContext as BackupRule;

            if (Rule == null)
                return;

            try
            {
                Utils.DoSingleBackup(Rule);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void btn_click_delete(object sender, RoutedEventArgs e)
        {
            BackupRule Rule = DataContext as BackupRule;

            if (Rule == null)
                return;

            Utils.DeleteRule(Rule);

            MainWindow.main.ShowPage(MainWindow.main.rulePage);
        }
    }
}
