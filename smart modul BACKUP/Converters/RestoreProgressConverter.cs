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
    /// Převádí RestoreInProgress.CurrentState a RestoreInProgress.Progress obnovy na hodnotu pro ProgressBarView.
    /// </summary>
    public class RestoreProgressConverter : IMultiValueConverter
    {
        private readonly static Dictionary<RestoreState, float> FROM = new Dictionary<RestoreState, float>()
        {
            { RestoreState.Starting, 0 },
            { RestoreState.ConnectingSftp, 0.05f },
            { RestoreState.ConnectingSql, 0.1f },
            { RestoreState.DownloadingZip, 0.15f },
            { RestoreState.ExtractingZip, 0.5f },
            { RestoreState.RestoringSources, 0.6f },
            { RestoreState.Finishing, 0.9f },
            { RestoreState.Done, 1 }
        };

        private readonly static Dictionary<RestoreState, float> TO = new Dictionary<RestoreState, float>()
        {
            { RestoreState.Starting, 0.05f },
            { RestoreState.ConnectingSftp, 0.1f },
            { RestoreState.ConnectingSql, 0.15f },
            { RestoreState.DownloadingZip, 0.5f },
            { RestoreState.ExtractingZip, 0.6f },
            { RestoreState.RestoringSources, 0.9f },
            { RestoreState.Finishing, 1f },
            { RestoreState.Done, 1 }
        };

        private float lastProgs = -1;
        private RestoreState lastState = RestoreState.Starting;

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var state = (RestoreState)values[0]; //RestoreInProgress.CurrentState
            var progr = (float)values[1]; //RestoreInProgress.Progress

            try
            {
                if (progr >= lastProgs && state != lastState)
                    return FROM[state];

                float from = FROM[state];
                return from + (TO[state] - from) * progr;
            }
            finally
            {
                lastProgs = progr;
                lastState = state;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
