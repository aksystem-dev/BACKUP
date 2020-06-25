using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SmartModulBackupClasses.Rules
{
    /// <summary>
    /// Představuje proces, který se má spustit a dokončit před vyhodnocením pravidla
    /// </summary>
    public class ProcessToStart
    {
        /// <summary>
        /// Název procesu
        /// </summary>
        public string ProcessName { get; set; }

        /// <summary>
        /// Argumenty oddělené mezerou
        /// </summary>
        public string Arguments { get; set; }

        /// <summary>
        /// Pokud true, pravidlo se nevyhodnotí, pokud se proces úspěšně nedokončí;
        /// pokud false, pravidlo se vyhodnotí i když se proces nedokončí úspěšně
        /// </summary>
        public bool Require { get; set; }

        /// <summary>
        /// Čas v ms, po který se bude čekat na dokončení procesu
        /// </summary>
        public int Timeout { get; set; }

        [XmlIgnore]
        public bool selected { get; set; }
    }
}
