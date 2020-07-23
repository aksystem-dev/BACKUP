using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartModulBackupClasses.Mails
{
    public class MailFile
    {
        public string FilePath { get; set; }

        public Mail Mail { get; set; }

        public MailFile(Mail mail, string filename)
        {
            Mail = mail;
            FilePath = filename;
        }
    }
}
