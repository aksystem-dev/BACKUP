using SmartModulBackupClasses;
using SmartModulBackupClasses.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartModulBackupClasses
{
    /// <summary>
    /// Vytváří instance třídy SftpUploader s konfigurací podle aktuálního ConfigManageru.
    /// </summary>
    public class SftpUploaderFactory : IFactory<SftpUploader>
    {
        //public SftpConfig Config;

        public SftpUploaderFactory() { }

        /// <summary>
        /// Vrátí instanci SftpUploader. Pokud je PC napojeno na povolený plán (zjišťováno přes PlanManager), vrátí SftpUploader
        /// s příslušnými údaji. Pakliže ne, vrátí SftpUploader s údaji z configu.
        /// </summary>
        /// <returns></returns>
        public SftpUploader GetInstance()
        {
            //return new SftpUploader(Config.Adress, Config.Port, Config.Username, Config.Password.Value);
            var cfg_man = Manager.Get<ConfigManager>();
            var plan_man = Manager.Get<PlanManager>();
            if (plan_man?.Plan?.Enabled == true)
            {
                var sftp = plan_man.Sftp;
                try
                {
                    return new SftpUploader(sftp.Host, sftp.Port, sftp.Username, sftp.Password);
                }
                catch
                {
                    return null;
                }
            }
            else if (cfg_man?.Config?.SFTP != null)
            {
                var sftp = cfg_man.Config.SFTP;
                try
                {
                    return new SftpUploader(sftp.Host, sftp.Port, sftp.Username, sftp.Password.Value);
                }
                catch
                {
                    return null;
                }
            }
            else
                return null;
        }
    }
}
