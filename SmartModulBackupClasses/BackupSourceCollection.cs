using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace SmartModulBackupClasses
{
    /// <summary>
    /// Obsahuje informaci o zdrojích pro dané pravidlo
    /// </summary>
    public class BackupSourceCollection
    {
        /// <summary>
        /// Seznam zdrojů
        /// </summary>
        [XmlIgnore]
        public List<BackupSource> All = new List<BackupSource>();

        [XmlElement("Database")]
        public BackupSource[] Databases
        {
            //když se zeptáme na databáze, vrátíme ty zdroje s typem Database
            get => All.Where(f => f.type == BackupSourceType.Database).ToArray();
            set
            {
                if (value == null)
                    return;

                //pokud tuto hodnotu nastavujeme, musíme nejprve z listu odstranit všechny Database
                foreach (var src in All.ToArray())
                    if (src.type == BackupSourceType.Database)
                        All.Remove(src);

                //musíme se ujistit, že všechny nové hodnoty mají typ databáze
                foreach (var src in value)
                    src.type = BackupSourceType.Database;

                //a pak tam přidat ty, které jsme nastavili
                All.AddRange(value);
            }
        }

        [XmlElement("Directory")]
        public BackupSource[] Directories
        {
            //když se zeptáme na složky, vrátíme ty zdroje s typem Directory
            get => All.Where(f => f.type == BackupSourceType.Directory).ToArray();
            set
            {
                if (value == null)
                    return;

                //pokud tuto hodnotu nastavujeme, musíme nejprve z listu odstranit všechny Directory
                foreach (var src in All.ToArray())
                    if (src.type == BackupSourceType.Directory)
                        All.Remove(src);

                //musíme se ujistit, že všechny nové hodnoty mají typ directory
                foreach (var src in value)
                    src.type = BackupSourceType.Directory;

                //a pak tam přidat ty, které jsme nastavili
                All.AddRange(value);
            }
        }

        [XmlElement("File")]
        public BackupSource[] Files
        {
            //když se zeptáme na soubory, vrátíme ty zdroje s typem File
            get => All.Where(f => f.type == BackupSourceType.File).ToArray();
            set
            {
                if (value == null)
                    return;

                //pokud tuto hodnotu nastavujeme, musíme nejprve z listu odstranit všechny Files
                foreach (var src in All.ToArray())
                    if (src.type == BackupSourceType.File)
                        All.Remove(src);

                //musíme se ujistit, že všechny nové hodnoty mají typ directory
                foreach (var src in value)
                    src.type = BackupSourceType.File;

                //a pak tam přidat ty, které jsme nastavili
                All.AddRange(value);
            }
        }

        /// <summary>
        /// Zařídí, aby měly všechny zdroje unikátní id
        /// </summary>
        public void FixIds()
        {
            //vytvoříme seznam idů, které už jsme viděli
            List<string> seenIds = new List<string>();

            foreach(var src in All)
            {
                //pokud id není nastavený, nastaví se podle cesty
                if (src.id == null || src.id == "")
                    src.id = Path.GetFileNameWithoutExtension(src.path);

                //pokud už existuje zdroj s tímto idem, musíme ho trochu změnit
                while (seenIds.Contains(src.id))
                    src.id = src.id.Increment();

                //přidat id na seznam
                seenIds.Add(src.id);
            }
        }
    }

    //public enum ZipMode { nozip, zip, both }

}
