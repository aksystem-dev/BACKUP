using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SmartModulBackupClasses
{
    [DataContract]
    public struct Error
    {
        [DataMember]
        public string ErrorHeader;
        [DataMember]
        public string ErrorDetail;

        public Error(string header, string detail = "")
        {
            ErrorHeader = header;
            ErrorDetail = detail;
        }
    }
}
