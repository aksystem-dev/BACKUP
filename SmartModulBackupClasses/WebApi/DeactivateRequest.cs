using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartModulBackupClasses.WebApi
{
    public class DeactivateRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string PC_ID { get; set; }
    }
}
