using System;
using System.IO;
using System.Xml.Serialization;

namespace SmartModulBackupClasses
{
    public class SavedSource : ICloneable
    {
        public SavedSource()
        {
            
        }

        /// <summary>
        /// Jak se jmenuje soubor zdroje v zipu zálohy
        /// </summary>
        public string filename { get; set; }

        /// <summary>
        /// Název zdroje (je-li type == Database, udává název databáze, jinak název souboru / adresáře)
        /// </summary>
        public string sourcename => Path.GetFileName(sourcepath);

        /// <summary>
        /// Odkud se záloha vzala (je-li type == Database, udává název databáze, jinak cestu k zdrojovému souboru / adresáři)
        /// </summary>
        public string sourcepath { get; set; }

        public BackupSuccessLevel Success { get; set; } = BackupSuccessLevel.EverythingWorked;

        public string Error { get; set; } = null;

        public string ErrorDetail { get; set; } = null;

        [XmlIgnore]
        public string Title => Success == BackupSuccessLevel.EverythingWorked ? "Vše v pořádku" : Error;

        /// <summary>
        /// Typ zálohy (databáze/adresář/soubor)
        /// </summary>
        public BackupSourceType type { get; set; }

        public object Clone()
        {
            return new SavedSource()
            {
                filename = filename,
                sourcepath = sourcepath,
                type = type
            };
        }
    }
}
