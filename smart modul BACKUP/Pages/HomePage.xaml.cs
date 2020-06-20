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

        public HomePage()
        {
            InitializeComponent();

            cfg_man = Manager.Get<ConfigManager>();
            Manager.Get<PlanManager>().Loaded += HomePage_Loaded;
            showRelevant();
        }

        private void HomePage_Loaded(PlanManager obj)
        {
            showRelevant();
        }

        void showRelevant()
        {
            ConfigManager config = Manager.Get<ConfigManager>();
            if (config?.Config?.WebCfg != null)
            {
                pan_notLoggedIn.Visibility = Visibility.Hidden;
            }
            else
            {
                pan_notLoggedIn.Visibility = Visibility.Visible;
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
    }
}
