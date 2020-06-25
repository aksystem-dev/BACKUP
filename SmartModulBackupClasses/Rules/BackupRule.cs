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

        [XmlIgnore]
        public string path = null;

        public int LocalID { get; set; }
        public string Name { get; set; } = "Pravidlo";
        public BackupSourceCollection Sources { get; set; } = new BackupSourceCollection();
        public Conditions Conditions { get; set; } = new Conditions();
        public BackupConfig LocalBackups { get; set; } = new BackupConfig();
        public BackupConfig RemoteBackups { get; set; } = new BackupConfig();

        public List<ProcessToStart> ProcessesBeforeStart { get; set; } = new List<ProcessToStart>();

        /// <summary>
        /// Zdali se zálohy mají zipovat
        /// </summary>
        public bool Zip { get; set; } = true;

        [XmlAttribute]
        public bool Enabled { get; set; } = true;

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

        public DateTime LastExecution { get; set; } = DateTime.MinValue;
        public DateTime LastEdit { get; set; }

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
            return deser.Deserialize(new StringReader(xml)) as BackupRule;
        }

        public string ToXmlString()
        {
            var ser = new XmlSerializer(typeof(BackupRule));
            var writer = new StringWriter();
            ser.Serialize(writer, this);
            return writer.ToString();
        }
    }
}
