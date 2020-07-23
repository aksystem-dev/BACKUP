using SmartModulBackupClasses;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace smart_modul_BACKUP
{
    /// <summary>
    /// Převádí BackupState na názvy
    /// </summary>
    public class BackupStateConverter : IValueConverter
    {
        private readonly static Dictionary<BackupState, string> NAMES = new Dictionary<BackupState, string>()
        {
            { BackupState.Initializing, "INICIALIZACE" },
            { BackupState.RunningProcesses, "SPOUŠTĚNÍ PROCESŮ" },
            { BackupState.ConnectingSftp, "PŘIPOJOVÁNÍ K SFTP SERVERU" },
            { BackupState.ConnectingSql, "PŘIPOJOVÁNÍ K SQL DATABÁZI" },
            { BackupState.CreatingVss, "VYTVÁŘENÍ SHADOW COPY" },
            { BackupState.BackupSources, "ZÁLOHA ZDROJE" },
            { BackupState.ZipBackup, "ZIPOVÁNÍ ZÁLOHY" },
            { BackupState.SftpUpload, "NAHRÁVÁNÍ NA SERVER" },
            { BackupState.MovingToLocalFolder, "PŘESOUVÁNÍ DO LOKÁLNÍ SLOŽKY" },
            { BackupState.Cancelling, "ZÁLOHA SE RUŠÍ" },
            { BackupState.Finishing, "UKONČOVÁNÍ" },
            { BackupState.Done, "HOTOVO" },
            { BackupState.OneToOneBackups, "ZÁLOHY 1:1" }
        };


        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var state = (BackupState)value;

            return NAMES[state];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
