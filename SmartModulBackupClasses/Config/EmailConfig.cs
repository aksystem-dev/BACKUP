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
        public void AllPropertiesChanged(Action<Action> invoker = null)
        {
            if (PropertyChanged == null)
                return;

            invoker = invoker ?? new Action<Action>(a => a());
            var dgate = PropertyChanged;

            foreach (var prop in GetType().GetProperties())
            {
                if (prop.GetMethod != null && prop.GetMethod.IsPublic)
                    invoker(() => dgate(this, new PropertyChangedEventArgs(prop.Name)));
            }
        }


        /// <summary>
        /// Zdali je povoleno odesílání chyb přes e-mail.
        /// </summary>
        public bool SendErrors { get; set; } = false;

        /// <summary>
        /// Adresa, z níž posílámě maily.
        /// </summary>
        public string FromAddress { get; set; } = "";

        public List<string> ToAddresses { get; set; } = new List<string>();

        /// <summary>
        /// SMTP server.
        /// </summary>
        public string SmtpHost { get; set; } = "";

        /// <summary>
        /// Port pro SMTP.
        /// </summary>
        public int SmtpPort { get; set; } = 0;

        /// <summary>
        /// Heslo pro SMTP.
        /// </summary>
        public Pwd Password { get; set; } = new Pwd();

        /// <summary>
        /// Jestli důvěřovat všem certifikátům.
        /// </summary>
        public bool TrustAllCertificates { get; set; } = false;
        public event PropertyChangedEventHandler PropertyChanged;

        public EmailConfig Copy()
        {
            return MemberwiseClone() as EmailConfig;
        }
    }
}
