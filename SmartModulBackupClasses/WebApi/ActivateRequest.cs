using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartModulBackupClasses.WebApi
{
    public class ActivateRequest
    {
        public string PC_ID { get; set; }
        public string PC_Name { get; set; }
        public int? PlanID { get; set; }
    }
}
