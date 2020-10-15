using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SmartModulBackupClasses.WCF
{
    [DataContract]
    public class CommonFault
    {
        [DataMember]
        public string Type { get; set; }

        [DataMember]
        public string Message { get; set; }
    }
}
