using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartModulBackupClasses.Mails
{
    public class MailCallbackArgs
    {
        public Mail Mail { get; set; }
        public bool Success { get; set; }
        public Exception Exception { get; set; }
    }
}
