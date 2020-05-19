using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartModulBackupClasses
{
    /// <summary>
    /// Obsahuje informace o vyhodnocení pravidla. Už se nepoužívá, použijte třídu Backup
    /// </summary>
    [Obsolete("už se nepoužívá, použijte třídu Backup")]
    public class BackupRuleExecutionInfo : IHaveID
    {
        public int GetID() => ID;

        public int ID { get; set; }
        public int RuleID { get; set; }
        public string RuleName { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public bool Finished { get; set; }

        /// <summary>
        /// Chyby, které se při záloze vyskytly
        /// </summary>
        public BackupError[] Errors { get; set; }

        /// <summary>
        /// Zdali byla záloha úspěšná
        /// </summary>
        public bool OK { get; set; }
    }
}
