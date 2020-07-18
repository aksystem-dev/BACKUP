using smart_modul_BACKUP;
using smart_modul_BACKUP.Managers;
using SmartModulBackupClasses;
using SmartModulBackupClasses.Managers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace smart_modul_BACKUP
{
    /// <summary>
    /// Interakční logika pro RulePage.xaml
    /// </summary>
    public partial class RulePage : Page
    {
         BackupRuleLoader rules => Manager.Get<BackupRuleLoader>();

        public RulePage()
        {
            InitializeComponent();

            Manager.Get<AvailableDbLoader>().Load();
            DataContext = rules;

            //při kliknutí na + přidáme pravidlo
            //btn_addRule.Click += (_, __) => AddRule();
        }

        private void AddRule(BackupRuleType type)
        {
            //přidat pravidlo se zadaným jménem
            var newrule = new BackupRule()
            {
                Enabled = true,
            };

            newrule.LocalBackups.enabled = true;
            newrule.LocalBackups.MaxBackups = 5;
            newrule.RemoteBackups.enabled = true;
            newrule.RemoteBackups.MaxBackups = 50;

            newrule.RuleType = type;

            MainWindow.main.ShowPage(new SingleRulePage(newrule, true));
        }

        private void singleBackup(object sender, RoutedEventArgs e)
        {
            BackupRule Rule = (sender as FrameworkElement).DataContext as BackupRule;

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

        private void deleteRule(object sender, RoutedEventArgs e)
        {
            BackupRule Rule = (sender as FrameworkElement).DataContext as BackupRule;

            if (Rule == null)
                return;

            Utils.DeleteRule(Rule);
        }

        private void ruleClick(object sender, RoutedEventArgs e)
        {
            var rule = (sender as FrameworkElement)?.DataContext as BackupRule;
            if (rule != null)
                MainWindow.main.ShowPage(new SingleRulePage(rule));
        }


        private void rule_toggled(object sender, EventArgs e)
        {
            var rule = (sender as FrameworkElement)?.DataContext as BackupRule;
            if (rule != null)
                rules.Update(rule);
        }


        private void btn_click_addRule(object sender, RoutedEventArgs e)
        {
            AddRule(BackupRuleType.FullBackups);
            popup_ruleTypeSelection.IsOpen = false;
        }

        private void btn_click_addOneToOneRule(object sender, RoutedEventArgs e)
        {
            AddRule(BackupRuleType.OneToOne);
            popup_ruleTypeSelection.IsOpen = false;
        }

        private void btn_click_addProtectedFolderRule(object sender, RoutedEventArgs e)
        {
            AddRule(BackupRuleType.ProtectedFolder);
            popup_ruleTypeSelection.IsOpen = false;
        }

        private void btn_click_showPopup(object sender, RoutedEventArgs e)
        {
            popup_ruleTypeSelection.IsOpen = true;
        }

        private void btn_click_hidePopup(object sender, RoutedEventArgs e)
        {
            popup_ruleTypeSelection.IsOpen = false;
        }

    }
}
