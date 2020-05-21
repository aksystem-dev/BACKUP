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
    public class RestoreInProgress : ProgressMonitor, INotifyPropertyChanged
    {
        [DataMember]
        public List<Error> Errors { get; set; } = new List<Error>();

        public event Action AfterUpdateCalled;
        public event PropertyChangedEventHandler PropertyChanged;

        public void Update(string msg, float progress)
        {
            CurrentTask = msg;
            Progress = progress;
            AfterUpdateCalled?.Invoke();
        }

        public void Refresh()
        {
            foreach (var prop in GetType().GetProperties())
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop.Name));
        }

        public event EventHandler Completed;
        public void Complete() => Completed?.Invoke(this, null);
    }
}
