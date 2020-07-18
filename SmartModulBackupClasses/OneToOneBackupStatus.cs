using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartModulBackupClasses
{
    public class OneToOneBackupStatus
    {
        public bool LocalBackupSuccess { get; set; }
        public bool RemoteBackupSuccess { get; set; }

        public List<string> LocalBackupFiles { get; set; } = new List<string>();
        public List<string> RemoteBackupFiles { get; set; } = new List<string>();
    }
}
