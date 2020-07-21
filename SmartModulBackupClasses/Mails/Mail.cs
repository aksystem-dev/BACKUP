using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartModulBackupClasses.Mails
{
    public class Mail
    {
        /// <summary>
        /// Adresa, kam e-mail poslat.
        /// </summary>
        public string To { get; set; }

        /// <summary>
        /// Obsah mailu.
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Jestli je e-mail v HTML formátu.
        /// </summary>
        public bool Html { get; set; }
    }
}
