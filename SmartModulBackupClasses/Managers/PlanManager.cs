using SmartModulBackupClasses.WebApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartModulBackupClasses.Managers
{
    public class PlanManager
    {
        private SmbApiClient _api => Manager.Get<SmbApiClient>();

        public PlanXml Plan { get; set; }

        public SftpResponse Sftp { get; set; }

        public event Action<PlanManager> Loaded;

        public async Task<PlanManager> LoadAsync()
        {
            try
            {
                var hp = await _api.HelloAsync();
                Plan = hp.ActivePlan;

                if (Plan != null)
                {
                    var sftp = await _api.GetSftpAsync();
                    Sftp = sftp;
                }
                else
                    Sftp = null;
            }
            catch
            {
                Plan = null;
                Sftp = null;
            }

            Loaded?.Invoke(this);
            return this;
        }

        public PlanManager Load()
            => SMB_Utils.Sync(LoadAsync);
    }
}
