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

        public event Action AfterUpdateCalled;
        public event PropertyChangedEventHandler PropertyChanged;

        public void Update(string msg, float progress)
        {
            Progress = progress;
            CurrentTask = msg;
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
}
