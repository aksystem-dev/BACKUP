using SmartModulBackupClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartModulBackupClasses
{
    /// <summary>
    /// Vytváří instance třídy SftpUploader s předdefinovanou konfigurací.
    /// </summary>
    public class SftpUploaderFactory
    {
        public SftpConfig Config;

        public SftpUploaderFactory(SftpConfig config)
            => Config = config;

        public SftpUploader GetInstance()
        {
            return new SftpUploader(Config.Adress, Config.Port, Config.Username, Config.Password);
        }
    }
}
