using System;
using System.Collections.Generic;
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
        [XmlIgnore]
        public string path;

        public int LocalID { get; set; }
        public string Name { get; set; } = "Pravidlo";
        public BackupSourceCollection Sources { get; set; } = new BackupSourceCollection();
        public Conditions Conditions { get; set; } = new Conditions();
        public BackupConfig LocalBackups { get; set; } = new BackupConfig();
        public BackupConfig RemoteBackups { get; set; } = new BackupConfig();

        [XmlAttribute]
        public bool Enabled { get; set; } = true;

        public DateTime LastExecution { get; set; } = DateTime.MinValue;

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
    }
}
