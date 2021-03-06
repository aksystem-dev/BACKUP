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

        public BackupRule Rule => DataContext as BackupRule;
        public SingleRulePage(BackupRule rule, bool add = false)
        {
            this.add = add;
            DataContext = rule;
            
            if(add)
                rule.Name = $"Pravidlo {rules.ID + 1}"; //inicializace názvu

            InitializeComponent();

            //txt_rulename.IsEnabled = add;
            //txt_rulename.BorderThickness = add ? new Thickness(2) : new Thickness(0);
        }

        /// <summary>
        /// jestli pravidlo lze uložit
        /// </summary>
        /// <returns></returns>
        private bool can_save()
        {
            txt_rulename.Text = txt_rulename.Text.Trim();
            if (Rule.Name == "")
                MessageBox.Show("Pravidlo musí mít název.");
            else if (add && rules.Any(r => r.Name == Rule.Name))
                MessageBox.Show("Pravidlo s takovým názvem již existuje.");
            else if (!Rule.Name.All(ch => char.IsLetterOrDigit(ch) || ch == ' '))
                MessageBox.Show("Název pravidla může obsahovat pouze písmena, čísla a mezery.");
            else return true;
            return false;
        }

        /// <summary>
        /// Zpět
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_click_back(object sender, RoutedEventArgs e)
        {
            //pokud má pravidlo validní název, umožnit uživateli opustit stránku a uložit pravidlo (page_unloaded)
            if (can_save())
                MainWindow.main.Back();
        }

        /// <summary>
        /// Jednorázová záloha
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_click_backup(object sender, RoutedEventArgs e)
        {
            BackupRule Rule = DataContext as BackupRule;

            if (Rule == null || !can_save())
                return;

            try
            {
                rules.Update(this.Rule);
                Utils.DoSingleBackup(Rule);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        bool updateOnUnloaded = true;

        /// <summary>
        /// Odstranění pravidla při kliknutí na koš
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_click_delete(object sender, RoutedEventArgs e)
        {
            BackupRule Rule = DataContext as BackupRule;

            if (Rule == null)
                return;

            if (!add)
                Utils.DeleteRule(Rule);

            updateOnUnloaded = false;
            MainWindow.main.ShowPage(MainWindow.main.rulePage);
        }

        /// <summary>
        /// Když upouštíme stránku s pravidlem, chceme ho automaticky uložit
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void page_unloaded(object sender, RoutedEventArgs e)
        {
            if (!updateOnUnloaded)
                return;

            if (add)
            {
                Rule.path = Path.Combine(Const.RULES_FOLDER, Rule.Name + ".xml");
                rules.Add(Rule);
            }
            else
            {
                rules.Update(Rule);
            }
        }
    }
}
