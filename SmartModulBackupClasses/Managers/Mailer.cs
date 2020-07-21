using MimeKit;
using SmartModulBackupClasses.Config;
using SmartModulBackupClasses.Mails;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace SmartModulBackupClasses.Managers
{
    /// <summary>
    /// Odesílá e-maily.
    /// </summary>
    public class Mailer
    {
        private EmailConfig cfg => Manager.Get<ConfigManager>()?.Config?.

        private MimeMessage getMimeMsg(Mail mail)
        {

        }

        /// <summary>
        /// Odešle mail. Pokud se to nepodaří, vyplivne výjimku.
        /// </summary>
        public async Task SendDumbAsync()
        {
            SmtpClient c = new SmtpClient();
            
        }

        /// <summary>
        /// Odešle mail. Pokud se to nepodaří, přidá mail do fronty na odeslání.
        /// Vrací, jestli se to podařilo.
        /// </summary>
        public async Task<bool> SendSmartAsync(Action<MailCallbackArgs> sendCallback = null)
        {

        }

        /// <summary>
        /// Odešle maily ve frontě. Maily, které jsou odeslány úspěšně, budou z fronty odstraněny.
        /// </summary>
        public async Task SendPendingEmails(Action<MailCallbackArgs> sendCallback = null)
        {

        }
        
    }
}
