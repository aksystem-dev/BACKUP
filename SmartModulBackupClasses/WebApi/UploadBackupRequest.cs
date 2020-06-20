using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartModulBackupClasses.WebApi
{
    public class UploadBackupRequest
    {
        public Backup Backup { get; set; }
        public string PC_ID { get; set; }
        public int PlanID { get; set; }
    }
}
