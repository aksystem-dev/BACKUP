using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SmartModulBackupClasses.WebApi
{
    public class RequestRecord
    {
        public string Uri { get; set; }
        public HttpStatusCode? StatusCode { get; set; }
        public ApiError? ApiError { get; set; }
        public DateTime DateTime { get; set; }
        public object RequestSerializable { get; set; }
    }
}
