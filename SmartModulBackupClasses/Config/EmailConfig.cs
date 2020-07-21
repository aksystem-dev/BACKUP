using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartModulBackupClasses
{
    public class EmailConfig : INotifyPropertyChanged
    {
        private bool _sendErrors;
        private string _fromAddress;
        private string _smtpHost;
        private int _smtpPort;
        private Pwd _password;
        private bool _trustAllCertificates;

        private void propChanged(string prop)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        /// <summary>
        /// Zdali je povoleno odesílání chyb přes e-mail.
        /// </summary>
        public bool SendErrors
        {
            get => _sendErrors;
            set
            {
                if (value == _sendErrors)
                    return;

                _sendErrors = value;
                propChanged(nameof(SendErrors));
            }
        }

        /// <summary>
        /// Adresa, z níž posílámě maily.
        /// </summary>
        public string FromAddress
        {
            get => _fromAddress;
            set
            {
                if (value == _fromAddress)
                    return;

                _fromAddress = value;
                propChanged(nameof(FromAddress));
            }
        }

        /// <summary>
        /// SMTP server.
        /// </summary>
        public string SmtpHost
        {
            get => _smtpHost;
            set
            {
                if (value == _smtpHost)
                    return;

                _smtpHost = value;
                propChanged(nameof(SmtpHost));
            }
        }

        /// <summary>
        /// Port pro SMTP.
        /// </summary>
        public int SmtpPort
        {
            get => _smtpPort;
            set
            {
                if (value == _smtpPort)
                    return;

                _smtpPort = value;
                propChanged(nameof(SmtpPort));
            }
        }

        /// <summary>
        /// Heslo pro SMTP.
        /// </summary>
        public Pwd Password
        {
            get => _password;
            set
            {
                if (_password == value)
                    return;

                if (_password == null)
                    _password.PropertyChanged -= pwd_propChanged;

                if (value != null)
                    value.PropertyChanged += pwd_propChanged;

                _password = value;
            }
        }

        private void pwd_propChanged(object sender, PropertyChangedEventArgs e)
        {
            propChanged(nameof(Password));
        }

        /// <summary>
        /// Jestli důvěřovat všem certifikátům.
        /// </summary>
        public bool TrustAllCertificates
        {
            get => _trustAllCertificates;
            set
            {
                if (_trustAllCertificates == value)
                    return;

                _trustAllCertificates = value;
                propChanged(nameof(TrustAllCertificates));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
