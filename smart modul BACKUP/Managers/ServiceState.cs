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
using System.Threading;
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

        private WCF.SmartModulBackupCallbackHandler _callbackHandler = null;

        private void propChanged(string name)
        {
            App.dispatch(() =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            });
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
                propChanged(nameof(IsServiceConnected));
            }
        }

        public bool IsServiceRunning => serviceController?.Status == ServiceControllerStatus.Running;
        public bool IsServiceConnected => State == ServiceConnectionState.Connected;

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

        public void Setup()
        {
            State = ServiceConnectionState.NotInstalled;
            serviceController = GetService();
            if (serviceController == null)
                return;

            State = ServiceConnectionState.NotRunning;
            while (serviceController.Status == ServiceControllerStatus.StartPending)
                Thread.Sleep(500);
            if (serviceController.Status != ServiceControllerStatus.Running)
                return;

            State = ServiceConnectionState.NotConnected;
            
            try
            {
                connect();
            }
            catch (Exception ex)
            {
                SmbLog.Error("Problém při spouštění ServiceState", ex, LogCategory.GuiServiceClient);
                return;
            }

            State = ServiceConnectionState.Connected;
            return;
        }

        /// <summary>
        /// Pokusí se připojit ke službě a interaguje přitom s uživatelem pomocí MessageBoxů. Umožňuje tak
        /// službu nainstalovat, spustit, apod.
        /// </summary>
        public void SetupWithMessageBoxes(bool installAndRun = false, string installExe = null)
        {
            bool admin = Utils.AmIAdmin();

            //získat odkaz na ServiceController
            serviceController = GetService();

            State = ServiceConnectionState.NotInstalled;

            //pokud jsme admini, zjistit cestu k .exe služby
            string alreadyInstalledExe = null;
            if (admin)
                alreadyInstalledExe = Utils.GetServiceInstalledPath();

            //pokud jsme ServiceController nenašli, pravděpodobně to znamená, že služba není nainstalována
            //zeptat se uživatele jestli nainstalovat
            if (serviceController == null)
            {
                while (true)
                {
                    //pokud installAndRun == false, zeptat se uživatele, jestli službu nainstalovat
                    installAndRun = installAndRun ||
                        MessageBox.Show("Služba smart modul BACKUP není nainstalována.\n\nNainstalovat službu?",
                                "Problém", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;

                    //pokud uživatel odpoví, že ne, vypadnout z funkce
                    if (!installAndRun)
                        return;

                    //pokud nejsem admin, říct si o adminova oprávnění
                    if (!admin)
                    {
                        SmbLog.Info("Restartuji GUI jako správce, abych mohl nainstalovat službu.", null, LogCategory.GUI);
                        Utils.RestartAsAdmin(new string[] { "-autorun", "-force" });
                    }

                    //změnit stav na instalaci
                    State = ServiceConnectionState.Installing;

                    //pokud se instalace nepovede, vrátit
                    if (!Utils.InstallService(installExe))
                        return;

                    //získat odkaz na službu
                    serviceController = GetService();

                    //pokud jsme jej úspěšně získali, můžem pokračovat
                    if (serviceController != null)
                        break;

                    //jinak se zeptat uživatele co a jak
                    if (MessageBox.Show("Nainstalovanou službu nemohu najít. Instalovat znovu?", "Problém",
                        MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                        continue;
                    else
                        break;
                }
            }
            //pokud jsme našli ServiceController a služba je nainstalována jinde, než má být
            else if(admin && alreadyInstalledExe.Trim('"') != Path.GetFullPath(installExe))
            {
                bool reinstall = MessageBox.Show("Na tomto systému už existuje instalace služby smart modul BACKUP. Pro vytvoření nové instalace je třeba jí odstranit.\n\nMám to provést?",
                    "Problém", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

                if (reinstall)
                {
                    State = ServiceConnectionState.Uninstalling;
                    if (!Utils.InstallService(alreadyInstalledExe.Trim('"'), true))
                        return;

                    State = ServiceConnectionState.Installing;
                    if (!Utils.InstallService(installExe))
                        return;
                }
                else
                    return;
            }

            State = ServiceConnectionState.NotRunning;

            //pokud se služba spouští, počkat, až se spustí
            if (serviceController.Status == ServiceControllerStatus.StartPending)
            {
                SmbLog.Info("Spuštění gui: služba startuje, čekám na ní", null, LogCategory.GUI);
                State = ServiceConnectionState.Starting;

                do Thread.Sleep(500);
                while (serviceController.Status == ServiceControllerStatus.StartPending);
            }

            //pokud služba není spuštěna, zeptat se uživatele, jestli jí spustit
            //a spustit jí, pokud odpověď zní ano
            if (serviceController.Status != ServiceControllerStatus.Running)
            {
                installAndRun = installAndRun ||
                    MessageBox.Show("Služba momentálně neběží. Mám ji spustit?",
                           "Problém", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

                if (!installAndRun)
                    return;

                if (!admin)
                {
                    SmbLog.Info("Restartuji GUI jako správce, abych mohl spustit službu.", null, LogCategory.GUI);
                    Utils.RestartAsAdmin(new string[] { "-autorun", "-force" });
                }

                //pokud služba neběží a uživatel přitakal, pokusíme se jí spustit
                while (true)
                {
                    try
                    {
                        State = ServiceConnectionState.Starting;
                        startService();
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

            //zde se k službě pokusíme připojit.
            while (true)
                try
                {
                    connect();

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
        }

        private bool startService()
        {
            if (serviceController.Status == ServiceControllerStatus.Running)
                return true;

            serviceController.Start();
            serviceController.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromMilliseconds(10000));
            if (serviceController.Status == ServiceControllerStatus.Running)
            {
                State = ServiceConnectionState.NotConnected;
                return true;
            }

            return false;
        }

        private void connect()
        {
            State = ServiceConnectionState.Connecting;

            _callbackHandler = new WCF.SmartModulBackupCallbackHandler();
            _callbackHandler.OnServiceDisconnected += Callback_OnServiceDisconnected;
            var context = new InstanceContext(_callbackHandler);

            Client = new ServiceInterface.SmartModulBackupInterfaceClient(context);
            Client.Connect();

            State = ServiceConnectionState.Connected;
        }

        public bool TryConnect()
        {
            try
            {
                connect();
                return true;
            }
            catch (Exception ex)
            {
                SmbLog.Error("Chyba při připojování ke službě přes WCF", ex, LogCategory.GuiServiceClient);
                return false;
            }
        }

        /// <summary>
        /// Když na Callback objektu služba zavolá Goodbye(), jakože se vypíná
        /// </summary>
        private async void Callback_OnServiceDisconnected()
        {
            await Task.Delay(100);
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
                    Client.ReloadAsync();
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
        public async Task<bool> StopService()
        {
            if (!serviceController.CanStop)
                return false;

            try
            {
                State = ServiceConnectionState.Stopping;

                await Task.Run(() =>
                {
                    serviceController.Stop();
                    serviceController.WaitForStatus(ServiceControllerStatus.Stopped);
                });

                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                //nastavit State tak, aby odpovídal stavu služby

                if (serviceController.Status == ServiceControllerStatus.Stopped)
                    State = ServiceConnectionState.NotRunning;
                else if (serviceController.Status == ServiceControllerStatus.Running)
                {
                    if (IsServiceConnected)
                        State = ServiceConnectionState.Connected;
                    else
                        State = ServiceConnectionState.NotConnected;
                }
                else
                    State = ServiceConnectionState.NotInitialized;
                
                
            }
        }

        public BackupInProgress[] GetBackupsInProgress() => Client.GetBackupsInProgress();
        public RestoreInProgress[] GetRestoresInProgresses() => Client.GetRestoresInProgress();
    }

    public enum ServiceConnectionState
    {
        /// <summary>
        /// Instance ServiceState není inicializována.
        /// </summary>
        NotInitialized,

        /// <summary>
        /// Služba není nainstalována
        /// </summary>
        NotInstalled,
        
        /// <summary>
        /// Služba se instaluje
        /// </summary>
        Installing,

        /// <summary>
        /// Služba se odinstalovává
        /// </summary>
        Uninstalling,

        /// <summary>
        /// Služba neběží
        /// </summary>
        NotRunning,

        /// <summary>
        /// služba se spouští
        /// </summary>
        Starting,

        /// <summary>
        /// Služba je spuštěna, ale není připojena
        /// </summary>
        NotConnected,

        /// <summary>
        /// Služba se připojuje
        /// </summary>
        Connecting,

        /// <summary>
        /// Služba je připojena
        /// </summary>
        Connected,

        /// <summary>
        /// Služba se vypíná
        /// </summary>
        Stopping
    }
}
