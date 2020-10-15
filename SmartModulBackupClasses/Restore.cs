using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartModulBackupClasses
{
    /// <summary>
    /// Představuje sadu informací o obnově zálohy.
    /// </summary>
    public class Restore : IHaveID
    {
        /// <summary>
        /// cesta k zip souboru zálohy
        /// </summary>
        public string zip_path { get; set; }

        /// <summary>
        /// seznam zdrojů pro obnovu
        /// </summary>
        public SavedSource[] sources { get; set; }

        /// <summary>
        /// umístění onoho zipu
        /// </summary>
        public BackupLocation location { get; set; }

        /// <summary>
        /// id příslušné zálohy
        /// </summary>
        public int backupID { get; set; }

        /// <summary>
        /// id PC, k němuž záloha patří (pokud null, bere se toto PC)
        /// </summary>
        public string pcId { get; set; }

        public int GetID() => ID;

        public int ID;
    }
}
