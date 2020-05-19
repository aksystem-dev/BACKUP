using smart_modul_BACKUP;
using SmartModulBackupClasses;
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
        public RulePage()
        {
            InitializeComponent();

            LoadedStatic.LoadAvailableDatabases();
            DataContext = LoadedStatic.rules;

            //při kliknutí na + přidáme pravidlo
            //btn_addRule.Click += (_, __) => AddRule();
        }

        private void AddRule()
        {
            var dialog = new InputDialog()
            {
                PromptText = "NÁZEV PRAVIDLA"
            };

            //zobrazíme dialog; pokud uživatel zmáčknul "cancel", vykašlem se na to
            if (dialog.ShowDialog() != true)
                return;

            //přidat pravidlo se zadaným jménem
            LoadedStatic.rules.Add(new BackupRule()
            {
                Enabled = true,
                Name = dialog.UserInput,
                path = $"Rules/{dialog.UserInput}.xml"
            });

            //id pravidla by měl nastavit RuleIdController, který MainWindow pověsil na CollectionChanged událost LoadedStatic.rules
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

            //Rule.SaveSelf();

            //if (LoadedStatic.service.State == ServiceConnectionState.Connected)
            //{
            //    LoadedStatic.service.client.Reload();
            //    LoadedStatic.service.client.DoSingleBackup(Rule.LocalID);
            //}
            //else
            //    MessageBox.Show("Služba není připojena, nelze provést jednorázovou zálohu.");
        }

        private void deleteRule(object sender, RoutedEventArgs e)
        {
            BackupRule Rule = (sender as FrameworkElement).DataContext as BackupRule;

            if (Rule == null)
                return;

            Utils.DeleteRule(Rule);

            //var dialog = new YesNoDialog()
            //{
            //    PromptText = "ODSTRANIT PRAVIDLO?"
            //};

            //bool? result = dialog.ShowDialog();

            //if (result == true)
            //{
            //    File.Delete(Rule.path);
            //    LoadedStatic.rules.Remove(Rule);
            //}
        }

        private void ruleClick(object sender, RoutedEventArgs e)
        {
            var rule = (sender as FrameworkElement)?.DataContext as BackupRule;
            if (rule != null)
                MainWindow.main.ShowPage(new SingleRulePage(rule));
        }

        private void btn_click_addRule(object sender, RoutedEventArgs e)
        {
            AddRule();
        }
    }
}
