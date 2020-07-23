using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartModulBackupClasses.Mails
{
    /// <summary>
    /// Informace o tom, jak se podařilo odeslání mailu.
    /// </summary>
    public class MailCallbackArgs
    {
        /// <summary>
        /// Objekt mailu
        /// </summary>
        public Mail Mail { get; set; }

        /// <summary>
        /// Zdali to byl úspěch
        /// </summary>
        public bool Success { get; set; }

        public Exception Exception { get; set; }

        /// <summary>
        /// Pokud jsme posílali jeden e-mail zvlášť každému příjemci, toto je seznam informací o tom,
        /// jak se to pro každého příjemce povedlo
        /// </summary>
        public Dictionary<string, MailMessageCallbackArgs> EachReceiverSuccess { get; set; }
    }

    public class MailMessageCallbackArgs
    {
        public bool Success { get; set; }
        public Exception Exception { get; set; }
    }
}
