using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace smart_modul_BACKUP
{
    public class ServiceStatusConverter : IValueConverter
    {
        public string ConnectedText { get; set; }
        public string NotConnectedText { get; set; }
        public string NotRunningText { get; set; }
        public string NotInstalledText { get; set; }
        public string NotInitializedText { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var state = (ServiceConnectionState)value;

            switch(state)
            {
                case ServiceConnectionState.Connected: return ConnectedText;
                case ServiceConnectionState.NotConnected: return NotConnectedText;
                case ServiceConnectionState.NotRunning: return NotRunningText;
                case ServiceConnectionState.NotInstalled: return NotInstalledText;
                case ServiceConnectionState.NotInitialized: return NotInitializedText;
                default: return "Ajta krajta...";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
