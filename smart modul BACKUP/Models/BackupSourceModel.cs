using smart_modul_BACKUP.Models;
using SmartModulBackupClasses;
using System.ComponentModel;
using System.Xml.Serialization;

namespace smart_modul_BACKUP
{
    /// <summary>
    /// Obal na BackupSource pro Binding na GUI.
    /// </summary>
    public class BackupSourceModel : INotifyPropertyChanged
    {
        public BackupSourceModel self => this;
        public BackupSourceModel()
        {
            //ve chvíli, kdy se změní jakákoliv vlastnost, chceme zahlásit, že se změnila tato instance
            //toto je ochcávka
            PropertyChanged += (_, args) =>
            {
                if (args.PropertyName != nameof(self))
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(self)));
            };
        }

        private bool _selected = false;

        /// <summary>
        /// Jestli je tato instance vybraná (pro složky - jestli je checkbox zaškrtnutý, pro databáze - jestli je u databáze specifikováno, jestli zálohovat nebo ne)
        /// </summary>
        public bool selected
        {
            get => _selected;
            set
            {
                if (value == _selected)
                    return;

                _selected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(selected)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEnabled)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsDisabled)));
            }
        }

        public bool IsEnabled => selected && source.enabled;
        public bool IsDisabled => selected && !source.enabled;

        private BackupSource _source;

        public BackupSource source
        {
            get => _source;
            set
            {
                if (_source != null)
                    _source.PropertyChanged -= _sourcePropertyChanged;

                _source = value;
                _source.PropertyChanged += _sourcePropertyChanged;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void _sourcePropertyChanged(object src, PropertyChangedEventArgs args)
        {
            if(args.PropertyName == nameof(source.enabled))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEnabled)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsDisabled)));
            }
        }
        
        public AvailableDatabase DbInfo { get; set; }
    }

    //public enum ZipMode { nozip, zip, both }

}
