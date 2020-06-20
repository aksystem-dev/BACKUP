using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SmartModulBackupClasses.WebApi
{
    /// <summary>
    /// Odpověď na /api/Client/Hello?...
    /// </summary>
    public class HelloResponse
    {
        /// <summary>
        /// Seznam dostupných plánů.
        /// </summary>
        public List<PlanXml> AvailablePlans { get; set; }

        /// <summary>
        /// Je-li na tomto počítači aktivovaný plán, toto udává jeho index v seznamu AvailablePlans. Není-li, bude to -1.
        /// </summary>
        public int ActivePlanIndex { get; set; }

        public PlanXml ActivePlan
        {
            get
            {
                if (ActivePlanIndex >= 0)
                    return AvailablePlans[ActivePlanIndex];
                return null;
            }
        }
    }
}
