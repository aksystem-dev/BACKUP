using Renci.SshNet;
using SmartModulBackupClasses;
using SmartModulBackupClasses.Managers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace SmartModulBackupClasses
{
    /// <summary>
    /// Vytváří instance třídy SftpUploader s konfigurací podle aktuálního ConfigManageru.
    /// </summary>
    public class SftpUploaderFactory : IFactory<SftpUploader>
    {
        private readonly AccountManager _accountManager;
        private readonly string _logfile = $"sftpUploaderFactoryLog_{Assembly.GetEntryAssembly().GetName().Name}.txt";

        private Dictionary<SftpClient, int> _clients = new Dictionary<SftpClient, int>();
        private SftpClient _currentClient = null;

        //public SftpConfig Config;

        public SftpUploaderFactory(AccountManager accountManager) 
        {
            this._accountManager = accountManager;
            _lastState = accountManager.State;
            _accountManager.StateChanged += _accountManager_StateChanged;

            createSftpClient();
        }

        private void log(string msg)
        {
            using (StreamWriter sw = new StreamWriter(_logfile, true))
                sw.WriteLine($"{DateTime.Now.ToString("dd.MM.yy HH:mm:ss")} {msg}");
            SmbLog.Info(msg, null, LogCategory.SFTP);
        }

        private LoginState _lastState; //poslední stav accountmanageru
        private void _accountManager_StateChanged(object sender, EventArgs e)
        {
            if (_accountManager.State == _lastState)
                return;

            _lastState = _accountManager.State;

            createSftpClient();
        }

        /// <summary>
        /// Nastaví nového SftpClienta podle AccountManageru.
        /// </summary>
        private void createSftpClient()
        {
            var cfg_man = Manager.Get<ConfigManager>();

            if (_accountManager.State == LoginState.LoginSuccessful)
            {
                log("AccountManager logged in, changing SftpClient");

                var sftp = _accountManager.SftpInfo;
                _currentClient = new SftpClient(sftp.Host, sftp.Port, sftp.Username, sftp.Password);
                _clients.Add(_currentClient, 0);
            }
            else if (_accountManager.State == LoginState.Offline && cfg_man?.Config?.SFTP != null)
            {
                log("AccountManager logged out, changing SftpClient");

                var sftp = cfg_man.Config.SFTP;
                _currentClient = new SftpClient(sftp.Host, sftp.Port, sftp.Username, sftp.Password.Value);
                _clients.Add(_currentClient, 0);
            }
            else
            {
                log("_currentClient = null");

                _currentClient = null;
            }
        }

        /// <summary>
        /// Připojí se k aktuálnímu klientovi (pokud připojený ještě není) a vrátí ho. 
        /// Přičte počítač použití pro daného klienta.
        /// </summary>
        /// <returns></returns>
        private SftpClient getCurrentClient()
        {
            if (_currentClient == null)
                return null;

            lock (_currentClient)
            {
                log($"a new user for SftpClient {_currentClient.GetHashCode()}; new user count: {_clients[_currentClient] + 1}");

                _clients[_currentClient]++;
                if (!_currentClient.IsConnected)
                {
                    log($"client {_currentClient} isn't connected. Connecting...");
                    _currentClient.Connect();
                }

                return _currentClient;
            }
        }

        /// <summary>
        /// Odpojí se od aktuálního klienta (pokud ho nikdo jiný nepoužívá).
        /// Odečte počítač použití pro daného klienta.
        /// Disposne ho pokud už se nepoužívá a zároveň není aktuální (_currentClient != client)
        /// </summary>
        /// <param name="client"></param>
        private void sftpClientDisconnect(SftpClient client)
        {
            lock (client)
            {
                log($"SftpClient {client.GetHashCode()} lost a user; new user count: {_clients[client] - 1}");

                if (--_clients[client] == 0)
                {
                    log($"No one is using SftpClient {client.GetHashCode()}. Disconnecting... ");

                    client.Disconnect();
                    if (_currentClient != client)
                    {
                        log($"SftpClient {client.GetHashCode()} is old. Disposing... ");

                        _clients.Remove(client);
                        client.Dispose();
                    }
                }
            }
        }

        /// <summary>
        /// Vrátí instanci SftpUploader. Pokud je PC napojeno na povolený plán (zjišťováno přes PlanManager), vrátí SftpUploader
        /// s příslušnými údaji. Pakliže ne, vrátí SftpUploader s údaji z configu.
        /// </summary>
        /// <returns></returns>
        public SftpUploader GetInstance()
        {
            try
            {
                var client = getCurrentClient();

                if (client == null)
                    return null;

                return new SftpUploader(client, () => sftpClientDisconnect(client));
            }
            catch (Exception ex)
            {
                SmbLog.Error("Chyba při pokusu o připojení k SFTP", ex, LogCategory.SFTP);
                throw;
            }
        }
    }
}
