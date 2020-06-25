using SmartModulBackupClasses;
using SmartModulBackupClasses.Managers;
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
    /// Interakční logika pro SingleRulePage.xaml
    /// </summary>
    public partial class SingleRulePage : Page
    {
        private readonly bool add;

        BackupRuleLoader rules => Manager.Get<BackupRuleLoader>();

        BackupRule rule => DataContext as BackupRule;
        public SingleRulePage(BackupRule rule, bool add = false)
        {
            this.add = add;
            DataContext = rule;
            
            if(add)
                rule.Name = $"Pravidlo {rules.ID + 1}";

            InitializeComponent();

            txt_rulename.IsEnabled = add;
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
                rules.Update(rule);
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

        private void page_unloaded(object sender, RoutedEventArgs e)
        {
            if (add)
            {
                rule.path = Path.Combine(Const.RULES_FOLDER, rule.Name + ".xml");
                rules.Add(rule);
            }
            else
            {
                rules.Update(rule);
            }
        }
    }
}
