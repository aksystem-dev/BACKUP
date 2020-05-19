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
    public class ServiceState : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private List<Restore> _restoresInProgress = new List<Restore>();
        public Restore[] RestoresInProgress => _restoresInProgress.ToArray();

        private ServiceConnectionState _state = ServiceConnectionState.NotInitialized;
        public ServiceConnectionState State
        {
            get => _state;
            private set
            {
                if (value == _state)
                    return;

                _state = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(State)));
            }
        }

        private ServiceInterface.SmartModulBackupInterfaceClient client = null;

        private ServiceController serviceController;

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
                    var callback = new WCF.SmartModulBackupCallbackHandler() { client = client };
                    callback.OnServiceDisconnected += Callback_OnServiceDisconnected;
                    var context = new InstanceContext(callback);

                    client = new ServiceInterface.SmartModulBackupInterfaceClient(context);
                    //client.Open();
                    client.Connect();

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
            client.Close();
            State = ServiceConnectionState.NotRunning;
        }

        public void Disconnect()
        {
            if (this.State != ServiceConnectionState.Connected)
                return;

            if (client.State == CommunicationState.Opened)
            {
                client.Disconnect();
                client.Close();
            }

            State = ServiceConnectionState.NotConnected;
        }

        /// <summary>
        /// Řekne službě, aby znovu načetla konfigurační soubory a znovu naplánovala vyhodnocování pravidel.
        /// </summary>
        public void Reload()
        {
            if (client == null || client.State != CommunicationState.Opened)
                return;

            //restartovat službu, je-li připojena
            if (this.State == ServiceConnectionState.Connected)
                try
                {
                    client.Reload();
                }
                catch
                {

                }
        }

        /// <summary>
        /// Řekne službě, aby provedla obnovu zálohy.
        /// </summary>
        /// <param name="restore"></param>
        public async void Restore(Restore restore)
        {
            //přidat objekt obnovy do seznamu a informovat o změně
            _restoresInProgress.Add(restore);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RestoresInProgress)));

            //počkat, až služba provede obnovu
            var response = await client.RestoreAsync(restore);

            //odstranit objekt obnovy ze seznamu a informovat o změně
            _restoresInProgress.Remove(restore);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RestoresInProgress)));

            //informovat uživatele o hotové obnově
            RestoreCompleteBubble(response);
        }

        public void RestoreCompleteBubble(RestoreResponse response)
        {
            if (response.Success)
                LoadedStatic.notifyIcon?.ShowBalloonTip(2000, "Data úspěšně obnovena", "Obnova dokončena", 
                    System.Windows.Forms.ToolTipIcon.Info);
            else if (response.SuccessfulRestoreSourceIndexes.Any())
                LoadedStatic.notifyIcon?.ShowBalloonTip(2000, "Obnova dokončena, ale došlo k chybám", "Obnova dokončena",
                    System.Windows.Forms.ToolTipIcon.Info);
            else
                LoadedStatic.notifyIcon?.ShowBalloonTip(2000, "Obnova dat se nezdařila", "Obnova selhala",
                    System.Windows.Forms.ToolTipIcon.Info);
        }

        public void DoSingleBackup(BackupRule rule)
        {
            LoadedStatic.SaveConfig();
            rule.SaveSelf();
            client.Reload();
            client.DoSingleBackup(rule.LocalID);
        }
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
