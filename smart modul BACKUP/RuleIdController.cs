using SmartModulBackupClasses;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smart_modul_BACKUP
{
    /// <summary>
    /// Přiděluje pravidlům ID.
    /// </summary>
    [Obsolete("Nepoužívá se.")]
    class RuleIdController
    {
        private string _idfile;
        public RuleIdController(string idfile)
        {
            _idfile = idfile;
        }

        public void Init(IEnumerable<BackupRule> rules)
        {
            int maxid = rules.Any() ? rules.Max(f => f.LocalID) : 0;
            if (maxid > GetCurrentId())
                SetCurrentId(maxid);
        }

        public int GetCurrentId()
        {
            if (!File.Exists(_idfile))
                return 0;
            else
                return int.Parse(File.ReadAllText(_idfile));
        }

        public void SetCurrentId(int id)
        {
            File.WriteAllText(_idfile, id.ToString());
        }

        /// <summary>
        /// Zkontroluje id daného pravidla
        /// </summary>
        /// <param name="rule"></param>
        public void TicketsPlease(BackupRule rule)
        {
            int id = GetCurrentId() + 1;
            SetCurrentId(id);
            rule.LocalID = id;
        }
    }
}
