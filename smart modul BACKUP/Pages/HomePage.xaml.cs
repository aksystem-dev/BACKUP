using SmartModulBackupClasses;
using SmartModulBackupClasses.Managers;
using SmartModulBackupClasses.WebApi;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace smart_modul_BACKUP
{
    /// <summary>
    /// Interakční logika pro HomePage.xaml
    /// </summary>
    public partial class HomePage : Page, INotifyPropertyChanged
    {
        public ConfigManager cfg_man { get; set; } //odkaz na ConfigManager
        public AccountManager plan_man { get; set; } //odkaz na AccountManager


        private string _zabraneMistoStr;

        /// <summary>
        /// Toto se zobrazí v textovém poli "Zabrané místo: "
        /// </summary>
        public string ZabraneMistoStr
        {
            get => _zabraneMistoStr;
            set
            {
                if (value == _zabraneMistoStr)
                    return;
                _zabraneMistoStr = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ZabraneMistoStr)));
            }
        }

        private string _totalBackupsFromThisPc;
        private string _totalServerBackupsFromThisPc;
        private string _totalLocallyAvailableBackupsFromThisPc;

        /// <summary>
        /// Toto se zobrazí v textovém poli "počet záloh z tohoto pc: "
        /// </summary>
        public string TotalBackupsFromThisPc
        {
            get => _totalBackupsFromThisPc;
            set
            {
                if (_totalBackupsFromThisPc == value)
                    return;
                _totalBackupsFromThisPc = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalBackupsFromThisPc)));
            }
        }

        /// <summary>
        /// Toto se zobrazí v textovém poli "počet serverových záloh: "
        /// </summary>
        public string TotalServerBackupsFromThisPc
        {
            get => _totalServerBackupsFromThisPc;
            set
            {
                if (_totalServerBackupsFromThisPc == value)
                    return;
                _totalServerBackupsFromThisPc = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalServerBackupsFromThisPc)));
            }
        }

        /// <summary>
        /// Toto se zobrazí v textovém poli "počet lokálních záloh: "
        /// </summary>
        public string TotalLocallyAvailableBackupsFromThisPc
        {
            get => _totalLocallyAvailableBackupsFromThisPc;
            set
            {
                if (_totalLocallyAvailableBackupsFromThisPc == value)
                    return;
                _totalLocallyAvailableBackupsFromThisPc = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalLocallyAvailableBackupsFromThisPc)));
            }
        }

        private readonly ByteSizeToStringConverter byteSizeToStringConverter;

        public HomePage()
        {
            //získat odkazy na hodící se objekty
            cfg_man = Manager.Get<ConfigManager>();
            plan_man = Manager.Get<AccountManager>();

            Loaded += HomePage_Loaded; //když se stránka načte, zavolej HomePage_Loaded
            plan_man.AfterLoginCalled += Plan_man_AfterLoginCalled; //když se změní uživatel, zavolej 

            InitializeComponent();

            //odkaz na konverter z počtu bytů na příjemnější jednotky
            byteSizeToStringConverter = Resources["conv_niceByte"] as ByteSizeToStringConverter;

            showRelevant(); //skrýt co chceme skrýt, zobrazit co chceme zobrazit, v závislosti na situaci
        }

        private void Plan_man_AfterLoginCalled(object sender, EventArgs e)
        {
            showRelevant();
            Task.Run(measureSftpFolder);
            Task.Run(countBackups);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void showRelevant()
        {
            if (plan_man.State == LoginState.Offline)
            {
                pan_plan.Visibility = Visibility.Collapsed;
                pan_customSftp.Visibility = Visibility.Visible;

            }
            else
            {
                pan_plan.Visibility = Visibility.Visible;
                pan_customSftp.Visibility = Visibility.Collapsed;
            }
        }

        private void HomePage_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Run(measureSftpFolder);
            Task.Run(countBackups);
        }

        /// <summary>
        /// Zjistí velikost složky pro ukládání dat na sftp serveru a uloží výsledek do ZabraneMistoStr
        /// </summary>
        private void measureSftpFolder()
        {
            App.dispatch(() => ZabraneMistoStr = "zjišťuji...");

            SftpUploader sftp = null;

            try
            {
                sftp = Manager.Get<SftpUploader>();

                sftp.Connect();
                var size = sftp.GetDirSize(plan_man.SftpFolder);
                ZabraneMistoStr = byteSizeToStringConverter.Convert(size, null, null, null).ToString();
            }
            catch (Exception ex)
            {
                App.dispatch(() => ZabraneMistoStr = "nepodařilo se zjistit");
                SmbLog.Error("problém při zjišťování množství dat uložených na serveru", ex, LogCategory.GUI);
            }
            finally
            {
                if (sftp?.IsConnected == true)
                    try
                    {
                        sftp.Disconnect();
                    }
                    catch { }
            }
        }

        /// <summary>
        /// Spočítá počet záloh a dá to do TotalBackupsFromThisPc, TotalLocallyAvailableBackupsFromThisPc a TotalServerBackupsFromThisPc
        /// </summary>
        /// <returns></returns>
        private async Task countBackups()
        {
            App.dispatch(() =>
                TotalBackupsFromThisPc = TotalLocallyAvailableBackupsFromThisPc = TotalServerBackupsFromThisPc = "zjišťuji...");

            try
            {
                var bkinfo_man = Manager.Get<BackupInfoManager>();
                await bkinfo_man.LoadAsync();
                var bks = bkinfo_man.LocalBackups;
                App.dispatch(() =>
                {
                    TotalBackupsFromThisPc = bks.Count().ToString();
                    TotalLocallyAvailableBackupsFromThisPc = bks.Where(b => b.AvailableOnThisComputer).Count().ToString();
                    TotalServerBackupsFromThisPc = bks.Where(b => b.AvailableRemotely).Count().ToString();
                });
            }
            catch (Exception ex)
            {
                App.dispatch(()
                    => TotalBackupsFromThisPc = TotalLocallyAvailableBackupsFromThisPc = TotalServerBackupsFromThisPc = "nepodařilo se zjistit");
                SmbLog.Error("problém při počítání záloh", ex, LogCategory.GUI);
            }
        }


    }
}
