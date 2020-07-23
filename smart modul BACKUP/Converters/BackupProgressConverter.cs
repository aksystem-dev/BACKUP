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
    /// Převádí vlastnosti BackupProgressMonitoru na vlastnost Progress pro ProgressBarView.
    /// </summary>
    class BackupProgressConverter : IMultiValueConverter
    {
        private readonly static Dictionary<BackupState, float> FROM = new Dictionary<BackupState, float>()
        {
            { BackupState.Initializing, 0 },
            { BackupState.RunningProcesses, 0.05f },
            { BackupState.ConnectingSftp, 0.1f },
            { BackupState.ConnectingSql, 0.12f },
            { BackupState.CreatingVss, 0.14f },
            { BackupState.BackupSources, 0.2f },
            { BackupState.ZipBackup, 0.5f },
            { BackupState.SftpUpload, 0.6f },
            { BackupState.MovingToLocalFolder, 0.9f },
            { BackupState.Cancelling, 0.95f },
            { BackupState.Finishing, 0.95f },
            { BackupState.Done, 1 },
            { BackupState.OneToOneBackups, 0.2f }
        };

        private readonly static Dictionary<BackupState, float> TO = new Dictionary<BackupState, float>()
        {
            { BackupState.Initializing, 0.05f },
            { BackupState.RunningProcesses, 0.1f },
            { BackupState.ConnectingSftp, 0.12f },
            { BackupState.ConnectingSql, 0.14f },
            { BackupState.CreatingVss, 0.2f },
            { BackupState.BackupSources, 0.5f },
            { BackupState.ZipBackup, 0.6f },
            { BackupState.SftpUpload, 0.9f },
            { BackupState.MovingToLocalFolder, 0.95f },
            { BackupState.Cancelling, 1f },
            { BackupState.Finishing, 1f },
            { BackupState.Done, 1 },
            { BackupState.OneToOneBackups, 0.95f }
        };

        private float lastProgs = -1;
        private BackupState lastState = BackupState.Initializing;

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var state = (BackupState)values[0]; //BackupInProgress.CurrentState
            var progr = (float)values[1]; //BackupInProgress.Progress

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
