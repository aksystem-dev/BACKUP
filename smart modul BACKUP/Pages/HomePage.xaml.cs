using SmartModulBackupClasses;
using SmartModulBackupClasses.Managers;
using SmartModulBackupClasses.WebApi;
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
    /// Interakční logika pro HomePage.xaml
    /// </summary>
    public partial class HomePage : Page
    {
        public ConfigManager cfg_man { get; set; }
        public PlanManager plan_man { get; set; }

        public HomePage()
        {
            InitializeComponent();

            cfg_man = Manager.Get<ConfigManager>();
            plan_man = Manager.Get<PlanManager>();
            plan_man.Loaded += HomePage_Loaded;
            showRelevant();
        }

        private void HomePage_Loaded(PlanManager obj)
        {
            showRelevant();
        }

        void showRelevant()
        {
            if (!plan_man.UseConfig)
            {
                pan_notLoggedIn.Visibility = Visibility.Hidden;
                pan_loggedIn.Visibility = Visibility.Visible;
            }
            else
            {
                pan_notLoggedIn.Visibility = Visibility.Visible;
                pan_loggedIn.Visibility = Visibility.Hidden;
            }
        }

        private void click_login(object sender, RoutedEventArgs e)
        {
            App.ShowLogin(false);
            Task.Run(async () =>
            {
                Manager.Get<BackupRuleLoader>().Load();
                await Manager.Get<BackupInfoManager>().LoadAsync();
            });
        }

        private void click_logout(object sender, RoutedEventArgs e)
        {
            App.Logout();
        }
    }
}
