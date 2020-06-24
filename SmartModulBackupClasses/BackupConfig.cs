﻿using System.Xml.Serialization;

namespace SmartModulBackupClasses
{
    public class BackupConfig
    {
        /// <summary>
        /// Maximální počet záloh (uplatněno pouze pokud LimitBackups == true)
        /// </summary>
        public int MaxBackups { get; set; } = 0;

        /// <summary>
        /// Jestli omezit maximální počet záloh (odstraňovat zálohy, které počet převyšují)
        /// </summary>
        public bool LimitBackups { get; set; } = true;

        [XmlAttribute]
        public bool enabled { get; set; } = true;

        /// <summary>
        /// Vrátí, zdali by to chtělo odstranit danou zálohu
        /// </summary>
        /// <param name="bk">Odkaz na info o záloze</param>
        /// <param name="index">Pořadí zálohy v seznamu záloh seřazeného sestupně podle data zálohy</param>
        /// <returns></returns>
        public bool ShouldDelete(Backup bk, int index)
        {
            if (index >= MaxBackups)
                return true;

            return false;
        }
    }

    //public enum ZipMode { nozip, zip, both }

}
