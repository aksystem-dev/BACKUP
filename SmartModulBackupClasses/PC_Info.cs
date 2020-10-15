using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SmartModulBackupClasses
{
    /// <summary>
    /// Informace o počítači
    /// </summary>
    public class PC_Info
    {
        /// <summary>
        /// Název PC
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// ID počítače (zpravidla shodné s FolderName)
        /// </summary>
        public string ProductID { get; set; }

        /// <summary>
        /// Co se zobrazí v UI jako název počítače
        /// </summary>
        public string DisplayName => Name ?? ProductID ?? RemoteFolderName;

        /// <summary>
        /// Název složky na SFTP serveru, kam se ukládají data z tohoto PC. <br />
        /// Zpravidla je to ID počítače. <br />
        /// Neserializuje se do XML. <br />
        /// </summary>
        [XmlIgnore]
        public string RemoteFolderName { get; set; }

        /// <summary>
        /// Cesta ke složce na SFTP serveru.
        /// </summary>
        public string RemoteFolderPath => SMB_Utils.GetRemotePCDirectory(RemoteFolderName);

        /// <summary>
        /// zdali se jedná o tento PC
        /// </summary>
        public bool IsThis => this.ProductID == This.ProductID;

        /// <summary>
        /// Info o tomto PC
        /// </summary>
        public static readonly PC_Info This =
            new PC_Info()
            {
                ProductID = SMB_Utils.GetComputerId(),
                RemoteFolderName = SMB_Utils.GetComputerId(),
                Name = SMB_Utils.GetComputerName()
            };

        private static readonly XmlSerializer _serializer
            = new XmlSerializer(typeof(PC_Info));

        /// <summary>
        /// Serializuje tuto instanci PC_Info na XML.
        /// </summary>
        /// <returns></returns>
        public string ToXML()
        {
            using (var writer = new StringWriter())
            {
                _serializer.Serialize(writer, this);
                return writer.ToString();
            }
        }

        /// <summary>
        /// Deserializuje PC_Info ze XML
        /// </summary>
        /// <param name="xml"></param>
        /// <returns></returns>
        public static PC_Info FromXML(string xml)
        {
            using (var reader = new StringReader(xml))
                return (PC_Info)_serializer.Deserialize(reader);
        }

        public override int GetHashCode()
        {
            return (ProductID ?? RemoteFolderName).GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is PC_Info pc)
                return (pc.ProductID ?? RemoteFolderName) == (this.ProductID ?? RemoteFolderName);
            return false;
        }
    }
}
