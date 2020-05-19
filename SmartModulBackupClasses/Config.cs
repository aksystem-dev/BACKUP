using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Data.SqlClient;

namespace SmartModulBackupClasses
{
    public class Config
    {
        public DatabaseConfig Connection { get; set; } = new DatabaseConfig();

        public SftpConfig SFTP { get; set; } = new SftpConfig();

        public string LocalBackupDirectory { get; set; } = "Backups";
        public string RemoteBackupDirectory { get; set; } = "Backups";
        public bool UseShadowCopy { get; set; } = false;

        //[XmlIgnore]
        //public TimeSpan _scheduleInterval = new TimeSpan(1, 0, 0);
        //public string ScheduleInterval
        //{
        //    get => _scheduleInterval.ToString(@"hh\:mm\:ss");
        //    set => _scheduleInterval = TimeSpan.Parse(value);
        //        //TimeSpan.ParseExact(value, @"hh\:mm\:ss", CultureInfo.CurrentCulture);
        //}

        //public List<BackupRule> Rules = new List<BackupRule>();

        /// <summary>
        /// Načte třídu konfigurace z textu XML.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static Config FromXML(string text)
        {
            XmlSerializer ser = new XmlSerializer(typeof(Config));
            using (StringReader reader = new StringReader(text))
                return ser.Deserialize(reader) as Config;
        }


        /// <summary>
        /// Převede objekt na XML řetězec.
        /// </summary>
        /// <returns></returns>
        public string ToXML()
        {
            XmlSerializer ser = new XmlSerializer(typeof(Config));
            using (StringWriter writer = new StringWriter())
            {
                ser.Serialize(writer, this);
                return writer.ToString();
            }
        }

    }

    public class DatabaseConfig
    {
        public string Server { get; set; } = @"?";
        public bool IntegratedSecurity { get; set; } = false;
        public string Username { get; set; } = "?";
        public string Password { get; set; } = "?";
        //public string PasswordHash
        //{
        //    //get => password.Encrypt();
        //    //set => password = value.Decrypt();
        //    get => Convert.ToBase64String(
        //        ProtectedData.Protect(Encoding.UTF8.GetBytes(Password), null, DataProtectionScope.LocalMachine));
        //    set => Password = Encoding.UTF8.GetString(
        //        ProtectedData.Unprotect(Convert.FromBase64String(value), null, DataProtectionScope.LocalMachine));
        //}

        public string GetConnectionString(int timeout)
        {
            var strbuilder = new SqlConnectionStringBuilder();
            strbuilder.DataSource = Server;
            strbuilder.InitialCatalog = "Master";
            strbuilder.IntegratedSecurity = IntegratedSecurity;
            strbuilder.ConnectTimeout = timeout;
            if(!IntegratedSecurity)
            {
                strbuilder.UserID = Username;
                strbuilder.Password = Password;
            }
            return strbuilder.ConnectionString;
        }
    }

    public class SftpConfig
    {
        public string Adress { get; set; } = "localhost";
        public int Port { get; set; } = 22;
        public string Username { get; set; } = "username";
        public string Password { get; set; } = "password";
    }
}
