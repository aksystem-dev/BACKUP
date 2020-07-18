using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace smart_modul_BACKUP
{
    /// <summary>
    /// Info o databázi.
    /// </summary>
    public class Database : INotifyPropertyChanged
    {
        [XmlText]
        public string Name { get; set; }

        [XmlAttribute]
        public bool Include { get; set; }

        private bool _isnew = false;

        public event PropertyChangedEventHandler PropertyChanged;

        [XmlAttribute]
        public bool IsNew
        {
            get => _isnew;
            set
            {
                _isnew = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsNew)));
            }
        }
    }
}
