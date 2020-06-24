using SmartModulBackupClasses.WebApi;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartModulBackupClasses.Managers
{
    public class PlanManager : INotifyPropertyChanged
    {
        public Action<Action> PropertyChangedDispatcher = null;
        private SmbApiClient _api => Manager.Get<SmbApiClient>();
        private ConfigManager _config => Manager.Get<ConfigManager>();

        public PlanXml Plan { get; set; }

        public SftpResponse Sftp { get; set; }

        /// <summary>
        /// zdali místo tohoto použít sftp připojení z configu
        /// </summary>
        public bool UseConfig { get; set; }

        public event Action<PlanManager> Loaded;
        public event PropertyChangedEventHandler PropertyChanged;

        public PlanManager()
        {
        }

        private void invokeLoaded()
        {
            PropertyChangedDispatcher?.Invoke(() =>
            {
                Loaded?.Invoke(this);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Plan)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Sftp)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UseConfig)));
            });
        }

        /// <summary>
        /// použije zadaný plán a stáhne přes api sftp údaje (počítá se s tím, že jsme tomu dali již správný plán)
        /// </summary>
        /// <param name="plan"></param>
        /// <returns></returns>
        public async Task<PlanManager> SetPlanAsync(PlanXml plan)
        {
            Plan = plan;
            if (Plan != null)
            {
                var sftp = await _api.GetSftpAsync(); //stáhnout z api přístupy k sftp
                Sftp = sftp;

                SMB_Log.Log("PlanManager: gotten sftp credentials - " + sftp?.ToString() ?? "null");
            }
            else
            {
                SMB_Log.Log("PlanManager: plan is null for some reason");
                Sftp = null;
            }

            return this;
        }

        /// <summary>
        /// načte plán přes api a stáhne sftp údaje
        /// </summary>
        /// <returns></returns>
        public async Task<PlanManager> LoadAsync()
        {
            if (_config?.Config?.WebCfg?.Online != true) 
                //pokud nemáme explicitně zadáno, že jsme připojeni na web
            {
                SMB_Log.Log("PlanManager: use config instead");

                UseConfig = true;
                Sftp = null;
                Plan = null;
                invokeLoaded();
                return this;
            }

            try
            {
                var hp = await _api.HelloAsync(); //stáhnout z api info o aktuálním plánu
                await SetPlanAsync(hp.ActivePlan);
            }
            catch (Exception ex)
            {
                SMB_Log.LogEx(ex);
                SMB_Log.Log("PlanManager: downloading info about plan failed...");
                Plan = null;
                Sftp = null;
            }

            UseConfig = false;
            invokeLoaded();
            return this;
        }

        public PlanManager Load()
            => SMB_Utils.Sync(LoadAsync);
    }
}
