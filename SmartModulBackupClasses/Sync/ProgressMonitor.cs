using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SmartModulBackupClasses
{
    /// <summary>
    /// Base class pro RestoreInProgress a BackupInProgress
    /// </summary>
    [DataContract]
    public class ProgressMonitor
    {
        [DataMember]
        public string Parameter { get; set; }
        [DataMember]
        public float Progress { get; set; }
        [DataMember]
        public int ProgressId;
    }
}
