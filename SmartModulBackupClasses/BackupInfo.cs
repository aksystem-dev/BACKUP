using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartModulBackupClasses
{
    /// <summary>
    /// Obsahuje info o záloze (jednom konkrétním zdroji a jedné konkrétní destinaci); už se nepoužívá, použijte třídu Backup
    /// </summary>
    [Obsolete("už se nepoužívá, použijte třídu Backup")]
    public class BackupInfo : IHaveID
    {
        /// <summary>
        /// Čas vytvoření zálohy
        /// </summary>
        public DateTime DateTime;

        /// <summary>
        /// Jméno pravidla, které vytvořilo zálohu (jak se jmenovalo v tu dobu, kdy byla vytvořena)
        /// </summary>
        public string RuleName;

        /// <summary>
        /// Id pravidla, které zálohu vytvořilo (jak se jmenovalo, když byla vytvořena
        /// </summary>
        public int RuleId;

        /// <summary>
        /// Identifikátor zdroje - nemusí být číslo, ale pro každé pravidlo by měl být unikátní pro každý zdroj;
        /// mělo by být null, pokud tato záloha obsahuje všechny zdroje pravidla
        /// </summary>
        public string SourceId;

        /// <summary>
        /// Jestli tato záloha obsahuje všechny zdroje pravidla. Pokud je toto true, znamená to, že se jedná o zip soubor
        /// obsahující zálohy všech zdrojů daného pravidla. Pokud je toto false, znamená to, že se jedná o zip soubor obsahující
        /// zálohu pouze jednoho ze zdrojů (uveden ve vlastnosti SourceId)
        /// </summary>
        public bool AllSources = false;

        /// <summary>
        /// Cesta, kde byla záloha uložena (pro lokální zálohy má kořen, pro vzdálené nikolivěk)
        /// </summary>
        public string SavedPath;

        /// <summary>
        /// Id zálohy
        /// </summary>
        public int BackupId;

        /// <summary>
        /// Velikost .zip souboru zálohy
        /// </summary>
        public long Size;

        /// <summary>
        /// Zdali je záloha lokální (Local) nebo vzdálená (SFTP)
        /// </summary>
        public BackupLocation Where;

        /// <summary>
        /// Typ zdroje (Databáze / složka)
        /// </summary>
        public BackupSourceType Type;

        [DefaultValue(-1)]
        /// <summary>
        /// Id vyhodnocení pravidla
        /// </summary>
        public int RefRuleExecutionID = -1;

        public int GetID() => BackupId;
    }

    public enum BackupLocation { Local, SFTP }
}
