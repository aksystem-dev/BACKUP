using SmartModulBackupClasses;
using SmartModulBackupClasses.WebApi;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Net;
using System.Windows.Media;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using SmartModulBackupClasses.Managers;

namespace smart_modul_BACKUP
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window, INotifyPropertyChanged
    {
        public ObservableCollection<PlanXml> AvailablePlans
        {
            get;
            set;
        }
            = new ObservableCollection<PlanXml>();
        int selectedPlan = -1;

        public event PropertyChangedEventHandler PropertyChanged;
        public SmbApiClient api;

        void updatePlans()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AvailablePlans)));
        }


        public LoginWindow()
        {
            InitializeComponent();
        }

        public void SetPassword(string str)
        {
            pwd.Password = str;
        }
        
        void showValiError(string msg)
        {
            lbl_validate.Foreground = Brushes.Red;
            lbl_validate.Visibility = Visibility.Visible;
            lbl_validate.Content = msg;
        }

        void showValiSuccess(string msg)
        {
            lbl_validate.Foreground = Brushes.Lime;
            lbl_validate.Visibility = Visibility.Visible;
            lbl_validate.Content = msg;
        }

        private async void click_login(object sender, RoutedEventArgs e)
        {
            await Login();
        }

        private async Task Login()
        {
            if (pwd.Password == "" || txt_username.Text == "")
            {
                showValiError("Zadejte prosím oba přihlašovací údaje.");
                return;
            }

            btn_login.IsEnabled = false;

            api = new SmbApiClient(txt_username.Text, pwd.Password, ms_timeout: 1500);

            try
            {
                AvailablePlans.Clear();
                var hello = await api.HelloAsync();
                foreach (var plan in hello.AvailablePlans)
                    AvailablePlans.Add(plan);
                selectedPlan = hello.ActivePlanIndex;
                showValiSuccess("Přihlášení bylo úspěšné. Vyberte prosím plán.");
            }
            catch (HttpStatusException http_ex)
            {
                if (http_ex.StatusCode == HttpStatusCode.Unauthorized)
                    showValiError("Špatné přihlašovací údaje.");
                else
                    showValiError("Došlo k nějaké chybě, omlouváme se.");
            }
            catch
            {
                showValiError("Došlo k nějaké chybě, omlouváme se.");
            }

            //updatePlans();
            if (selectedPlan >= 0)
                lbx_plans.SelectedIndex = selectedPlan;
            else
                lbx_plans.SelectedItem = null;
            btn_login.IsEnabled = true;
        }

        private async void click_activate(object sender, RoutedEventArgs e)
        {
            if (lbx_plans.SelectedItem == null)
                return;

            var plan = lbx_plans.SelectedItem as PlanXml;
            if (!plan.Enabled)
            {
                MessageBox.Show("Tento plán ještě nebyl zprovozněn. Nelze ho aktivovat.");
                return;
            }

            try
            {
                await api.ActivateAsync(plan.ID);
            }
            catch (SmbApiException api_ex)
            {
                switch(api_ex.Error)
                {
                    case ApiError.AlreadyActivated:
                        break;
                    case ApiError.AuthFailed:
                        MessageBox.Show("Nepodařilo se ověření uživatele.");
                        return;
                    case ApiError.MaxClientsReached:
                        MessageBox.Show("Nelze překročit maximální počet klientů plánu.");
                        return;
                    default:
                        MessageBox.Show("Omlouváme se, ale došlo k chybě. Plán se nepodařilo aktivovat.");
                        return;
                }
            }
            catch
            {
                MessageBox.Show("Omlouváme se, ale došlo k chybě. Plán se nepodařilo aktivovat.");
                return;
            }

            var cfg_man = Manager.Get<ConfigManager>();
            var config = cfg_man.Config;
            if (config.WebCfg == null)
                config.WebCfg = new WebConfig();
            config.WebCfg.Username = api.Username;
            config.WebCfg.Password = new Pwd(api.Password);
            config.WebCfg.Offline = false;
            cfg_man.Save();

            DialogResult = true;
            Close();
        }

        PasswordBox pwd;

        private void store_pwd_obj(object sender, EventArgs e)
        {
            pwd = sender as PasswordBox;
        }

        private void win_loaded(object sender, RoutedEventArgs e)
        {
            if (txt_username.Text != null && pwd.Password != "")
                Login().Wait();
        }

        private void click_skip(object sender, RoutedEventArgs e)
        {
            var cfg_man = Manager.Get<ConfigManager>();
            var config = cfg_man.Config;
            if (config.WebCfg == null)
                config.WebCfg = new WebConfig();
            config.WebCfg.Offline = true;
            cfg_man.Save();
            api = null;
            DialogResult = true;
            Close();
        }
    }
}
