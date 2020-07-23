using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SmartModulBackupClasses
{
    [DataContract]
    public class BackupInProgress : ProgressMonitor, INotifyPropertyChanged
    {
        [DataMember]
        public string RuleName { get; set; }
        [DataMember]
        public int RuleId { get; set; }

        [DataMember]
        public List<Error> Errors { get; set; } = new List<Error>();

        [DataMember]
        public BackupState CurrentState { get; set; } = BackupState.Initializing;

        public event Action AfterUpdateCalled;
        public event PropertyChangedEventHandler PropertyChanged;

        public void Update(BackupState current, float progress, string msg = "")
        {
            CurrentState = current;
            Progress = progress;
            Parameter = msg;
            AfterUpdateCalled?.Invoke();
        }

        public void Refresh()
        {
            foreach (var prop in GetType().GetProperties())
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop.Name));
        }

        public event EventHandler Completed;
        public void Complete() => Completed?.Invoke(this, null);


        public object TAG;
    }

    public enum BackupState
    {
        Initializing,
        RunningProcesses,
        ConnectingSftp,
        ConnectingSql,
        CreatingVss,
        BackupSources,
        ZipBackup,
        SftpUpload,
        MovingToLocalFolder,
        Cancelling,
        Finishing,
        OneToOneBackups,
        Done
    }
}
