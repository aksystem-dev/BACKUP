using SmartModulBackupClasses.Rules;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SmartModulBackupClasses
{
    /// <summary>
    /// Zálohovací pravidlo
    /// </summary>
    public class BackupRule
    {
        /// <summary>
        /// Sem si můžeme uložit nějaká pomocná data.
        /// </summary>
        [XmlIgnore]
        public object TAG { get; set; }

        [XmlIgnore]
        public ObservableCollection<BackupInProgress> InProgress { get; set; }
            = new ObservableCollection<BackupInProgress>();
        
        /// <summary>
        /// Cesta, z níž bylo pravidlo načteno
        /// </summary>
        [XmlAttribute]
        public string path = null;

        /// <summary>
        /// Lokální ID unikátní pro tento PC
        /// </summary>
        public int LocalID { get; set; }

        /// <summary>
        /// Název pravidla (také by měl být na 1 PC unikítní)
        /// </summary>
        public string Name { get; set; } = "Pravidlo";

        /// <summary>
        /// Definice zdrojů pro zálohu
        /// </summary>
        public BackupSourceCollection Sources { get; set; } = new BackupSourceCollection();

        /// <summary>
        /// Podmínky spuštení v čase
        /// </summary>
        public Conditions Conditions { get; set; } = new Conditions();

        /// <summary>
        /// Nastavení lokálních záloh
        /// </summary>
        public BackupConfig LocalBackups { get; set; } = new BackupConfig();

        /// <summary>
        /// Nastavení záloh přes SFTP
        /// </summary>
        public BackupConfig RemoteBackups { get; set; } = new BackupConfig();

        /// <summary>
        /// Procesy pro spuštení před zahájením vyhodnocování pravidla
        /// </summary>
        public ObservableCollection<ProcessToStart> ProcessesBeforeStart { get; set; }
            = new ObservableCollection<ProcessToStart>();

        /// <summary>
        /// Zdali se zálohy mají zipovat
        /// </summary>
        public bool Zip { get; set; } = true;

        /// <summary>
        /// Zdali je automatické spuštění pravidla povoleno
        /// </summary>
        [XmlAttribute]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Typ záloh
        /// </summary>
        [XmlAttribute]
        public BackupRuleType RuleType { get; set; } = BackupRuleType.FullBackups;

        /// <summary>
        /// Jestli bylo pravidlo nahráno na server.
        /// </summary>
        [XmlAttribute]
        public bool Uploaded { get; set; } = false;

        /// <summary>
        /// Jestli bylo pravidlo staženo na klienta.
        /// </summary>
        [XmlAttribute]
        public bool Downloaded { get; set; } = false;

        /// <summary>
        /// Datum posledního vyhodnocení
        /// </summary>
        public DateTime LastExecution { get; set; } = DateTime.MinValue;

        /// <summary>
        /// Datum poselední úpravy
        /// </summary>
        public DateTime LastEdit { get; set; }

        /// <summary>
        /// Jestli soubory v cílovém umístění i odstraňovat. Pouze pro zálohy 1:1 (OneToOne)
        /// </summary>
        [DefaultValue(true)]
        public bool OneToOneDelete { get; set; } = true;

        /// <summary>
        /// Pokud true, pravidlo se nespustí tak, aby běželo víckrát najednou
        /// </summary>
        public bool DisableConcurrentExecution { get; set; } = false;

        /// <summary>
        /// zdali se má automaticky zaškrtnout "zálohovat" u nově přidaných databází
        /// </summary>
        public bool AutoBackupNewDatabases { get; set; } = false;

        public void SaveSelf()
        {
            XmlSerializer ser = new XmlSerializer(typeof(BackupRule));
            using (StreamWriter writer = new StreamWriter(path))
                ser.Serialize(writer, this);
        }

        public static BackupRule LoadFromXml(string file, bool autoSaveSelf = false)
        {
            XmlSerializer ser = new XmlSerializer(typeof(BackupRule));
            BackupRule rule = null;

            //načíst pravidlo
            using (StreamReader reader = new StreamReader(file))
                rule = ser.Deserialize(reader) as BackupRule;

            //pokud je z nějakého důvodu null, vrátit null
            if (rule == null) return null;

            //zařídit, aby bylo pravidlo správně
            rule.Sources.FixIds();

            if (autoSaveSelf)
                //uložit změny provedené Fix()
                using (StreamWriter writer = new StreamWriter(file, false))
                    ser.Serialize(writer, rule);

            //předat info o umístění
            rule.path = file;

            //vrátit pravidlo
            return rule;
        }

        public static BackupRule LoadFromXmlStr(string xml)
        {
            var deser = new XmlSerializer(typeof(BackupRule));
            var rule = deser.Deserialize(new StringReader(xml)) as BackupRule;
            rule.Sources.FixIds();
            return rule;
        }

        public string ToXmlString()
        {
            var ser = new XmlSerializer(typeof(BackupRule));
            var writer = new StringWriter();
            ser.Serialize(writer, this);
            return writer.ToString();
        }
    }

    public enum BackupRuleType
    {
        FullBackups,
        OneToOne,
        ProtectedFolder
    }
}
