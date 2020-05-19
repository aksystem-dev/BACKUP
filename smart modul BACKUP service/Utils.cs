using Renci.SshNet;
using smart_modul_BACKUP_service.WCF;
using SmartModulBackupClasses;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smart_modul_BACKUP_service
{
    public static class Utils
    {
        public static SmartModulBackupService Service;

        /// <summary>
        /// Vytváří instance SftpUploader s předdefinovanou konfigurací.
        /// </summary>
        public static SftpUploaderFactory SftpFactory;

        /// <summary>
        /// Vytváří instance SqlBackuper s předdefinovanou konfigurací.
        /// </summary>
        public static SqlBackuperFactory SqlFactory;
        public static XmlInfoLoaderSftpMirror<Backup> SavedBackups;
        public static Config Config { get; set; }
        //public static GUI gui;
        public static ISmartModulBackupInterfaceCallback GUIS;

        public static string lastBackupSavePath = "lastBackups.txt";

        public static async void DoAfter(TimeSpan delay, Action action)
        {
            await Task.Delay(delay);
            action();
        }
    }
}
