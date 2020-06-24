using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartModulBackupClasses.WebApi
{
    public class DeleteBackupRequest
    {
        public int PlanID { get; set; }
        public string PC_ID { get; set; }
        public int localBackupId { get; set; }
    }
}
