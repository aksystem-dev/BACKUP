using System.Xml.Serialization;

namespace SmartModulBackupClasses
{
    public class BackupConfig
    {
        public int MaxBackups { get; set; } = 0;
        [XmlAttribute]
        public bool enabled { get; set; } = true;
    }

    //public enum ZipMode { nozip, zip, both }

}
