using SmartModulBackupClasses;
using SmartModulBackupClasses.Managers;
using System;

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
            var cfg_man = Manager.Get<ConfigManager>();
            var plan_man = Manager.Get<AccountManager>();

            //pokud jsme připojeni na web, vrátit SftpUploader vytvořený pomocí přístupových údajů stažených přes web
            if (plan_man.State == LoginState.LoginSuccessful)
            {
                var sftp = plan_man.SftpInfo;
                try
                {
                    SmbLog.Info($"SftpUploaderFactory: returning SftpUploader({sftp.Host},{sftp.Port},{sftp.Username},{new string('*', sftp.Password.Length)}) from web plan", category: LogCategory.SFTP);
                    return new SftpUploader(sftp.Host, sftp.Port, sftp.Username, sftp.Password);
                }
                catch (Exception ex)
                {
                    SmbLog.Error($"SftpUploaderFactory: failed to get SftpUploader from web plan, returning null...", ex, LogCategory.SFTP);
                    return null;
                }
            }

            //jinak vrátít SftpUploader pomocí lokální konfigurace
            else if (plan_man.State == LoginState.Offline && cfg_man?.Config?.SFTP != null)
            {
                var sftp = cfg_man.Config.SFTP;
                try
                {
                    SmbLog.Info($"SftpUploaderFactory: returning SftpUploader({sftp.Host},{sftp.Port},{sftp.Username},{new string('*', sftp.Password.Value.Length)}) from config", category: LogCategory.SFTP);
                    return new SftpUploader(sftp.Host, sftp.Port, sftp.Username, sftp.Password.Value);
                }
                catch (Exception ex)
                {
                    SmbLog.Error($"SftpUploaderFactory: failed to get SftpUploader from config, returning null...", ex, LogCategory.SFTP);
                    return null;
                }
            }
            else
                return null;
        }
    }
}
