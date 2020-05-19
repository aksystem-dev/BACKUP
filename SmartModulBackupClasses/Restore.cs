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
        public string zip_path;

        /// <summary>
        /// seznam zdrojů pro obnovu
        /// </summary>
        public SavedSource[] sources;

        /// <summary>
        /// umístění onoho zipu
        /// </summary>
        public BackupLocation location;

        /// <summary>
        /// id příslušné zálohy
        /// </summary>
        public int backupID;

        public int GetID() => ID;

        public int ID;
    }
}
