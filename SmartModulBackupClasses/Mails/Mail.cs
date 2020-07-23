using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SmartModulBackupClasses.Mails
{
    public class Mail
    {
        private static readonly XmlSerializer xml = new XmlSerializer(typeof(Mail));

        /// <summary>
        /// Předmět mailu
        /// </summary>
        public string Subject { get; set; }

        /// <summary>
        /// Obsah mailu.
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Jestli je e-mail v HTML formátu.
        /// </summary>
        public bool Html { get; set; }

        /// <summary>
        /// Adresy, kam e-mail poslat.
        /// </summary>
        public List<string> ToAddresses { get; set; }

        public string ToXml()
        {
            using (var writer = new StringWriter())
            {
                xml.Serialize(writer, this);
                return writer.ToString();
            }
        }

        public static Mail DeXml(string str)
        {
            using (var reader = new StringReader(str))
                return xml.Deserialize(reader) as Mail;
        }

        public Mail Copy()
        {
            return MemberwiseClone() as Mail;
        }
    }
}
