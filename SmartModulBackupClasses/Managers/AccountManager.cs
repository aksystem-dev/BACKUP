using SmartModulBackupClasses.WebApi;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartModulBackupClasses.Managers
{
    public enum LoginState
    {
        /// <summary>
        /// Aplikaci používáme offline, nepojíme se na api.
        /// </summary>
        Offline,

        /// <summary>
        /// Aplikaci chcem používat online, ale připojení na api se nezdařilo.
        /// </summary>
        LoginFailed,

        /// <summary>
        /// Jsme napojeni na webové api
        /// </summary>
        LoginSuccessful
    }
    
    /// <summary>
    /// Napojuje se na webové api a stahuje z něj potřebné informace.
    /// </summary>
    public class AccountManager : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler AfterLoginCalled;
        public Action<Action> propertyChangedDispatcher;

        private void propsChanged()
        {
            var propertyChangedHandler = PropertyChanged;
            var afterLoginCalledHandler = AfterLoginCalled;

            if (propertyChangedHandler == null && afterLoginCalledHandler == null)
                return;

            var func = propertyChangedDispatcher ?? new Action<Action>(a => a());
            func(() =>
            {
                propertyChangedHandler?.Invoke(this, new PropertyChangedEventArgs(nameof(Api)));
                propertyChangedHandler?.Invoke(this, new PropertyChangedEventArgs(nameof(State)));
                propertyChangedHandler?.Invoke(this, new PropertyChangedEventArgs(nameof(PlanInfo)));
                propertyChangedHandler?.Invoke(this, new PropertyChangedEventArgs(nameof(SftpInfo)));
                afterLoginCalledHandler?.Invoke(this, null);

            });
        }

        /// <summary>
        /// Instance SmbApiClient pro komunikaci s Api
        /// </summary>
        public SmbApiClient Api { get; private set; }

        /// <summary>
        /// Stav připojení k api
        /// </summary>
        public LoginState State { get; private set; }

        /// <summary>
        /// Pokud se poslední přihlášení nezdařilo, zde je uložena výjimka, k níž došlo
        /// </summary>
        public Exception LoginException { get; private set; }

        /// <summary>
        /// Info o aktivním plánu
        /// </summary>
        public PlanXml PlanInfo => HelloInfo?.ActivePlan;

        /// <summary>
        /// Info o tomto klientovi
        /// </summary>
        public HelloResponse HelloInfo { get; private set; }

        /// <summary>
        /// Přístupy na Sftp dle aktivního plánu
        /// </summary>
        public SftpResponse SftpInfo { get; private set; }

        /// <summary>
        /// Vrátí složku, kam ukládat na server data. Jsme-li připojeni na účet na webu, je tato složka zjištěna
        /// přes api. Jsme-li offline, je zjištěna z Configu.
        /// </summary>
        public string SftpFolder
        {
            get
            {
                switch(State)
                {
                    case LoginState.LoginFailed:
                        throw new InvalidOperationException();
                    case LoginState.LoginSuccessful:
                        return SftpInfo.Directory;
                    case LoginState.Offline:
                        return Manager.Get<ConfigManager>().Config.SFTP.Directory;
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        /// <summary>
        /// Pokusí se připojit na webové api a stáhnout relevantní informace (aktivní plán, přístupy na sftp, ...)
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public async Task<bool> TryLoginWithAsync(WebConfig config)
        {
            try
            {
                await LoginWithAsync(config);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Připojí se na webové api a stáhne relevantní informace (aktivní plán, přístupy na sftp, ...)
        /// </summary>
        /// <param name="config"></param>
        public async Task LoginWithAsync(WebConfig config)
        {
            try
            {
                //pokud používáme aplikaci offline, na přihlašování se vykašleme
                if (config.Online == false)
                {
                    Api = null;
                    HelloInfo = null;
                    SftpInfo = null;
                    LoginException = null;
                    State = LoginState.Offline;
                    return;
                }

                try
                {
                    Api = new SmbApiClient(config.Username, config.Password.Value, SMB_Utils.GetComputerId(), SMB_Utils.GetComputerName());
                    HelloInfo = await Api.HelloAsync();
                    if (PlanInfo != null)
                        SftpInfo = await Api.GetSftpAsync();
                    LoginException = null;
                    State = LoginState.LoginSuccessful;
                }
                catch (Exception ex)
                {
                    Api = null;
                    HelloInfo = null;
                    SftpInfo = null;
                    LoginException = ex;
                    State = LoginState.LoginFailed;
                    throw ex;
                }
            }
            finally
            {
                propsChanged();
            }
        }

        public async Task RedownloadSftp()
        {
            SftpInfo = await Api.GetSftpAsync();
        }
    }

    //public class AccountManager : INotifyPropertyChanged
    //{
    //    public Action<Action> PropertyChangedDispatcher = null;

    //    private SmbApiClient _api => Manager.Get<SmbApiClient>();

    //    //public SmbApiClient Api
    //    //{
    //    //    get
    //    //    {
    //    //        if (_api == null)
    //    //            ConnectApi();

    //    //        return _api;
    //    //    }
    //    //    set => _api = value;
    //    //}

    //    private ConfigManager _config => Manager.Get<ConfigManager>();

    //    public PlanXml Plan { get; set; }

    //    public SftpResponse Sftp { get; set; }

    //    /// <summary>
    //    /// zdali místo tohoto použít sftp připojení z configu
    //    /// </summary>
    //    public bool UseConfig { get; private set; }
    //    public AccountState State { get; private set; }

    //    public event Action<AccountManager> Loaded;
    //    public event PropertyChangedEventHandler PropertyChanged;

    //    public string SftpDirectory => UseConfig ? _config.Config.SFTP.Directory : Sftp.Directory;

    //    public AccountManager()
    //    {
    //    }

    //    private void invokeLoaded()
    //    {
    //        PropertyChangedDispatcher?.Invoke(() =>
    //        {
    //            Loaded?.Invoke(this);
    //            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Plan)));
    //            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Sftp)));
    //            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UseConfig)));
    //            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(State)));
    //        });
    //    }

    //    /// <summary>
    //    /// použije zadaný plán a stáhne přes api sftp údaje (počítá se s tím, že jsme tomu dali již správný plán)
    //    /// </summary>
    //    /// <param name="plan"></param>
    //    /// <returns></returns>
    //    private async Task<AccountManager> setPlanAsync(PlanXml plan)
    //    {
    //        Plan = plan;
    //        if (Plan != null)
    //        {
    //            var sftp = await _api.GetSftpAsync(); //stáhnout z api přístupy k sftp
    //            Sftp = sftp;

    //            SMB_Log.Log("PlanManager: gotten sftp credentials - " + sftp?.ToString() ?? "null");
    //        }
    //        else
    //        {
    //            SMB_Log.Log("PlanManager: plan is null for some reason");
    //            Sftp = null;
    //        }

    //        invokeLoaded();
    //        return this;
    //    }

    //    /// <summary>
    //    /// načte plán přes api a stáhne sftp údaje
    //    /// </summary>
    //    /// <returns></returns>
    //    public async Task<AccountManager> LoadAsync(PlanXml plan = null)
    //    {
    //        if (_config?.Config?.WebCfg?.Online != true) 
    //            //pokud nemáme explicitně zadáno, že jsme připojeni na web
    //        {
    //            SMB_Log.Log("PlanManager: use config instead");

    //            UseConfig = true;
    //            Sftp = null;
    //            Plan = null;
    //            State = AccountState.NotLoggedIn;
    //            invokeLoaded();
    //            return this;
    //        }

    //        try
    //        {
    //            if (plan == null)
    //            {
    //                var hp = await _api.HelloAsync(); //stáhnout z api info o aktuálním plánu
    //                await setPlanAsync(hp.ActivePlan);
    //            }
    //            else
    //                await setPlanAsync(plan);
    //            State = AccountState.LoggedIn;
    //        }
    //        catch (Exception ex)
    //        {
    //            SMB_Log.LogEx(ex);
    //            SMB_Log.Log("PlanManager: downloading info about plan failed...");
    //            Plan = null;
    //            Sftp = null;
    //            State = AccountState.LoginFailed;
    //        }

    //        UseConfig = false;
    //        invokeLoaded();
    //        return this;
    //    }

    //    public AccountManager Load(PlanXml plan = null)
    //        => SMB_Utils.Sync(() => LoadAsync(plan));
    //}
}
