using System.ComponentModel;
using System.Xml.Serialization;

namespace SmartModulBackupClasses
{
    /// <summary>
    /// Zdroj pro zálohu
    /// </summary>
    public class BackupSource : INotifyPropertyChanged
    {
        private bool _enabled = false;

        /// <summary>
        /// Jestli je tento zdroj zálohy povolen.
        /// </summary>
        [XmlAttribute]
        public bool enabled
        {
            get => _enabled;
            set
            {
                if (value == _enabled)
                    return;

                _enabled = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(enabled)));
            }
        }

        [XmlText]
        public string path { get; set; }
        [XmlAttribute]
        public string id { get; set; } = "";
        [XmlIgnore]
        public BackupSourceType type { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    //public enum ZipMode { nozip, zip, both }

}
