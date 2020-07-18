using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartModulBackupClasses.WebApi
{
    /// <summary>
    /// Informace o plánu posílané přes webové api.
    /// </summary>
    public class PlanXml
    {
        /// <summary>
        /// Id plánu
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// Název tarifu
        /// </summary>
        public string TarifName { get; set; }

        /// <summary>
        /// Kapacita
        /// </summary>
        public double Kapacita { get; set; }

        /// <summary>
        /// Maximální počet klientů
        /// </summary>
        public int MaxClients { get; set; }

        /// <summary>
        /// Aktuální počet připojených klientů
        /// </summary>
        public int CurrentClients { get; set; }

        /// <summary>
        /// Je plán osobní?
        /// </summary>
        public bool Osobni { get; set; }

        /// <summary>
        /// Název firmy (pokud Osobni == false)
        /// </summary>
        public string FirmaName { get; set; }

        /// <summary>
        /// Jestli je plán použitelný (jestli už superadmin přidělil přístupy na SFTP)
        /// </summary>
        public bool Enabled { get; set; }

        public DateTime PlatnostDo { get; set; }
    }
}
