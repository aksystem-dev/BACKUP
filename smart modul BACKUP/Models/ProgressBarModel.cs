using SmartModulBackupClasses;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smart_modul_BACKUP
{
    /// <summary>
    /// Info o tom, v jakém stavu je vyhodnocování nějakého úkolu.
    /// </summary>
    [Obsolete("Místo tohoto se používá BackupInProgress, RestoreInProgress a ProgressMonitor.")]
    public class ProgressBarModel : INotifyPropertyChanged
    {
        public ProgressBarModel Me => this;
        public int ID { get; set; }

        private string _statemsg;
        private float _progress;

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler OnBeforeRemoved;
        public event EventHandler OnRemoved;

        private void _change(params string[] names) => names.ForEach(f => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(f)));

        public string StateMsg
        {
            get => _statemsg;
            set
            {
                _statemsg = value;
                _change(nameof(StateMsg));
            }
        }

        public float Progress
        {
            get => _progress;
            set
            {
                _progress = value;
                _change(nameof(Progress));
            }
        }

        public void Remove()
        {
            OnBeforeRemoved?.Invoke(this, new EventArgs());
            OnRemoved?.Invoke(this, new EventArgs());
        }
    }
}
