using SmartModulBackupClasses;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace smart_modul_BACKUP
{
    /// <summary>
    /// Umožňuje interakci s Windows smart modul BACKUP službou přes WCF rozhraní.
    /// </summary>
    public class ServiceState : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private List<Restore> _restoresInProgress = new List<Restore>();
        public Restore[] RestoresInProgress => _restoresInProgress.ToArray();

        private ServiceConnectionState _state = ServiceConnectionState.NotInitialized;

        private void propChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        /// <summary>
        /// Aktuální stav.
        /// </summary>
        public ServiceConnectionState State
        {
            get => _state;
            private set
            {
                if (value == _state)
                    return;

                _state = value;
                propChanged(nameof(State));
                propChanged(nameof(IsServiceRunning));
            }
        }

        public bool IsServiceRunning
        {
            get => State == ServiceConnectionState.NotConnected || State == ServiceConnectionState.Connected;
        }

        /// <summary>
        /// Odkaz na SmartModulBackupInterfaceClient pro komunikaci s WCF službou
        /// </summary>
        public ServiceInterface.SmartModulBackupInterfaceClient Client { get; private set; } = null;

        /// <summary>
        /// Odkaz na serviceController umožňující kontrolovat a řídit stav Windows služby
        /// </summary>
        private ServiceController serviceController;

        /// <summary>
        /// Vrátí ServiceController pro Windows službu s názvem smart modul BACKUP service
        /// </summary>
        /// <returns></returns>
        public ServiceController GetService()
        {
            return ServiceController.GetServices().FirstOrDefault(f => f.ServiceName == "smart modul BACKUP service");
        }

        /// <summary>
        /// Pokusí se připojit ke službě a interaguje přitom s uživatelem pomocí MessageBoxů.
        /// </summary>
        public void SetupWithMessageBoxes(bool installAndRun = false)
        {
            bool admin = Utils.AmIAdmin();

            //získat odkaz na ServiceController
            serviceController = GetService();

            State = ServiceConnectionState.NotInstalled;

            //pokud jsme ServiceController nenašli, pravděpodobně to znamená, že služba není nainstalována
            if (serviceController == null)
            {
                while (true)
                {
                    installAndRun = installAndRun ||
                        MessageBox.Show("Služba smart modul BACKUP není nainstalována.\n\nNainstalovat službu?",
                                "Problém", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;

                    if (!installAndRun)
                        return;

                    if (!admin)
                        Utils.RestartAsAdmin(new string[] { "-autorun", "-force" });

                    if (!Utils.InstallService())
                        return;

                    serviceController = GetService();

                    if (serviceController != null)
                        break;

                    if (MessageBox.Show("Nainstalovanou službu nemohu najít. Instalovat znovu?", "Problém",
                        MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                        continue;
                    else
                        break;
                }
            }

            State = ServiceConnectionState.NotRunning;

            if (serviceController.Status != ServiceControllerStatus.Running)
            {
                installAndRun = installAndRun ||
                    MessageBox.Show("Služba momentálně neběží. Mám ji spustit?",
                           "Problém", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

                if (!installAndRun)
                    return;

                if (!admin)
                    Utils.RestartAsAdmin(new string[] { "-autorun", "-force" });

                //pokud služba neběží a uživatel přitakal, pokusíme se jí spustit
                while (true)
                {
                    try
                    {
                        serviceController.Start();
                        serviceController.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromMilliseconds(10000));
                    }
                    catch (Exception e)
                    {
                        //pokud došlo k chybě, zeptáme se uživatele, jestli to chce zkusit znovu
                        if (MessageBox.Show($"Službu se nepodařilo spustit. Chyba ({e.GetType().Name})\n\n{e.Message}\n\nZkusit znovu?",
                            "Chyba", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
                            continue;
                        else
                            return;
                    }

                    if (serviceController.Status == ServiceControllerStatus.Running)
                        //pokud jsme službu nastartovali úspěšně, můžeme vylézt z while cyklu
                        break;
                    else
                    {
                        //jinak se zeptáme uživatele na jeho názor
                        if (MessageBox.Show($"Službu se nepodařilo spustit. Zkusit znovu?",
                            "Chyba", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
                            continue;
                        else
                            return;
                    }
                }
            }

            State = ServiceConnectionState.NotConnected;

            //zde se k službě pokusíme připojit.
            while (true)
                try
                {
                    var callback = new WCF.SmartModulBackupCallbackHandler() { client = Client };
                    callback.OnServiceDisconnected += Callback_OnServiceDisconnected;
                    var context = new InstanceContext(callback);

                    Client = new ServiceInterface.SmartModulBackupInterfaceClient(context);
                    //client.Open();
                    Client.Connect();

                    break;
                }
                catch (Exception e)
                {
                    var msg_res = MessageBox.Show($"Připojení ke službě se nezdařilo. (chyba {e.GetType().Name})\n\n {e.Message} \n\n Zkusit znovu?", "Problém",
                        MessageBoxButton.YesNo, MessageBoxImage.Warning);

                    if (msg_res == MessageBoxResult.Yes)
                        continue;
                    else
                        return;
                }

            State = ServiceConnectionState.Connected;
        }

        /// <summary>
        /// Když na Callback objektu služba zavolá Goodbye(), jakože se vypíná
        /// </summary>
        private void Callback_OnServiceDisconnected()
        {
            Client.Close();
            State = ServiceConnectionState.NotRunning;
        }

        /// <summary>
        /// Řekne službě, že se odpojujeme, a odpojí se
        /// </summary>
        public void Disconnect()
        {
            if (this.State != ServiceConnectionState.Connected)
                return;

            if (Client.State == CommunicationState.Opened)
            {
                Client.Disconnect();
                Client.Close();
            }

            State = ServiceConnectionState.NotConnected;
        }

        /// <summary>
        /// Řekne službě, aby znovu načetla konfigurační soubory a znovu naplánovala vyhodnocování pravidel.
        /// </summary>
        public void Reload()
        {
            if (Client == null || Client.State != CommunicationState.Opened)
                return;

            //restartovat službu, je-li připojena
            if (this.State == ServiceConnectionState.Connected)
                try
                {
                    Client.Reload();
                }
                catch (Exception ex)
                {
                    SmbLog.Error($"výj. v ServiceState.cs: {ex.GetType().Name}: \n{ex.Message}", ex, LogCategory.GuiServiceClient);
                }
        }

        /// <summary>
        /// Řekne službě, aby provedla obnovu zálohy.
        /// </summary>
        /// <param name="restore"></param>
        public RestoreInProgress StartRestore(Restore restore)
        {
            ////přidat objekt obnovy do seznamu a informovat o změně
            //_restoresInProgress.Add(restore);
            //PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RestoresInProgress)));

            //počkat, až služba provede obnovu
            return Client.Restore(restore);
        }

        /// <summary>
        /// Řekne službě, ať provede zálohu určitého pravidla. Vrátí odkaz na BackupInProgress, pomocí nějž lze
        /// monitorovat průběh zálohy.
        /// </summary>
        /// <param name="rule"></param>
        /// <returns></returns>
        public BackupInProgress DoSingleBackup(BackupRule rule)
        {
            rule.SaveSelf();
            //client.Reload();  -> o to ať se postará sama služba
            return Client.DoSingleBackup(rule.ToXmlString()); //odselat žádost přes WCF
        }

        /// <summary>
        /// Vypne službu.
        /// </summary>
        /// <returns></returns>
        public bool StopService()
        {
            if (!serviceController.CanStop)
                return false;

            try
            {
                serviceController.Stop();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public BackupInProgress[] GetBackupsInProgress() => Client.GetBackupsInProgress();
        public RestoreInProgress[] GetRestoresInProgresses() => Client.GetRestoresInProgress();
    }

    public enum ServiceConnectionState
    {
        NotInitialized,
        NotInstalled,
        NotRunning,
        NotConnected,
        Connected
    }
}
