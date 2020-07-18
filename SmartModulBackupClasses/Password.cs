using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SmartModulBackupClasses
{
    /// <summary>
    /// Ukládání hesel do XML
    /// </summary>
    public class Pwd : INotifyPropertyChanged
    {
        static readonly byte[] entropy = { 5, 10, 15, 10, 5, 1, 3, 4 };
        private string _hash;

        public event PropertyChangedEventHandler PropertyChanged;

        void pwdChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Hash)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
        }

        public static Encoding PasswordEncoding { get; set; } = Encoding.Unicode;

        [XmlText]
        public string Hash
        {
            get => _hash;
            set
            {
                if (value == _hash)
                    return;

                _hash = value;
                pwdChanged();
            }
        }

        [XmlIgnore]
        public string Value
        {
            get
            {
                try
                {
                    var enc_bytes = Convert.FromBase64String(Hash);
                    var bytes = ProtectedData.Unprotect(enc_bytes, entropy, DataProtectionScope.LocalMachine);
                    return PasswordEncoding.GetString(bytes);
                }
                catch (Exception ex)
                {
                    Value = "";
                    return "";
                }
            }
            set
            {
                var bytes = PasswordEncoding.GetBytes(value);
                var enc_bytes = ProtectedData.Protect(bytes, entropy, DataProtectionScope.LocalMachine);
                var hashed = Convert.ToBase64String(enc_bytes);
                if (_hash != hashed)
                {
                    _hash = hashed;
                    pwdChanged();
                }
            }
        }

        public Pwd() { }
        public Pwd(string val)
        {
            Value = val;
        }
    }
}
