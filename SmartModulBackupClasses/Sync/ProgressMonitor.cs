using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SmartModulBackupClasses
{
    [DataContract]
    public class ProgressMonitor
    {
        [DataMember]
        public string CurrentTask { get; set; }
        [DataMember]
        public float Progress { get; set; }
        [DataMember]
        public int ProgressId;
    }
}
