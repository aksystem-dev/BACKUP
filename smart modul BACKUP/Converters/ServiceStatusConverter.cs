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
    /// Podle aktuálního stavu ServiceState vrátí text popisující, co se právě děje.
    /// </summary>
    public class ServiceStatusConverter : IValueConverter
    {
        private readonly static Dictionary<ServiceConnectionState, string> NAMES = new Dictionary<ServiceConnectionState, string>()
        {
            { ServiceConnectionState.Connected, "SLUŽBA PŘIPOJENA" },
            { ServiceConnectionState.Connecting, "SLUŽBA SE PŘIPOJUJE" },
            { ServiceConnectionState.Installing, "SLUŽBA SE INSTALUJE" },
            { ServiceConnectionState.NotConnected, "SLUŽBA BĚŽÍ, ALE NENÍ PŘIPOJENA" },
            { ServiceConnectionState.NotInitialized, "STAV SLUŽBY NENÍ ZNÁM" },
            { ServiceConnectionState.NotRunning, "SLUŽBA NEBĚŽÍ" },
            { ServiceConnectionState.Starting, "SLUŽBA SE SPOUŠTÍ" },
            { ServiceConnectionState.Uninstalling, "SLUŽBA SE ODINSTALOVÁVÁ" },
            { ServiceConnectionState.NotInstalled, "SLUŽBA NENÍ NAINSTALOVÁNA" },
            { ServiceConnectionState.Stopping, "SLUŽBA SE VYPÍNÁ" }
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var state = (ServiceConnectionState)value;
            return NAMES[state];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
