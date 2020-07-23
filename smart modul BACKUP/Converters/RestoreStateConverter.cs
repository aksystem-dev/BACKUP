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
    public class RestoreStateConverter : IValueConverter
    {
        private readonly static Dictionary<RestoreState, string> NAMES = new Dictionary<RestoreState, string>()
        {
            { RestoreState.Starting, "INICIALIZACE" },
            { RestoreState.ConnectingSftp, "PŘIPOJENÍ K SFTP SERVERU" },
            { RestoreState.ConnectingSql, "PŘIPOJENÍ K SQL SERVERU" },
            { RestoreState.DownloadingZip, "STAHUJI ZIP" },
            { RestoreState.ExtractingZip, "EXTRAHUJI ZIP" },
            { RestoreState.RestoringSources, "OBNOVA ZDROJE" },
            { RestoreState.Done, "HOTOVO" },
            { RestoreState.Finishing, "DOKONČUJI" }
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return NAMES[(RestoreState)value];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
