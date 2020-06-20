using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.ComponentModel;

namespace SmartModulBackupClasses
{
    public class Config : INotifyPropertyChanged
    {
        private bool unsavedChanges;
        public bool UnsavedChanges
        {
            get => unsavedChanges;
            set
            {
                if (value == unsavedChanges)
                    return;

                unsavedChanges = value;

                if (Connection != null)
                    connection.UnsavedChanges = value;
                if (SFTP != null)
                    SFTP.UnsavedChanges = value;
                if (WebCfg != null)
                    WebCfg.UnsavedChanges = value;

                propChanged(nameof(UnsavedChanges));
            }
        }

        private bool useShadowCopy = false;
        private string remoteBackupDirectory = "Backups";
        private string localBackupDirectory = "Backups";
        private DatabaseConfig connection = new DatabaseConfig();
        private SftpConfig sFTP = new SftpConfig();
        private WebConfig webCfg;

        public Config()
        {
            PropertyChanged += Config_PropertyChanged;
        }

        private void Config_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(UnsavedChanges))
                UnsavedChanges = true;
        }

        private void propChanged(string prop)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        public DatabaseConfig Connection
        {
            get => connection;
            set
            {
                if (value == connection)
                    return;

                if (connection != null)
                    connection.PropertyChanged -= Config_PropertyChanged;

                if (value != null)
                    value.PropertyChanged += Config_PropertyChanged;

                connection = value;
            }
        }

        public SftpConfig SFTP
        {
            get => sFTP;
            set
            {
                if (value == sFTP)
                    return;

                if (sFTP != null)
                    sFTP.PropertyChanged -= Config_PropertyChanged;

                if (value != null)
                    value.PropertyChanged += Config_PropertyChanged;

                sFTP = value;
            }
        }

        public WebConfig WebCfg
        {
            get => webCfg; 
            set
            {
                if (value == webCfg)
                    return;

                if (webCfg != null)
                    webCfg.PropertyChanged -= Config_PropertyChanged;

                if (value != null)
                    value.PropertyChanged += Config_PropertyChanged;

                webCfg = value;
            }
        }

        public string LocalBackupDirectory
        {
            get => localBackupDirectory;
            set
            {
                if (value == localBackupDirectory)
                    return;

                localBackupDirectory = value;
                propChanged(nameof(LocalBackupDirectory));
            }
        }
        public string RemoteBackupDirectory
        {
            get => remoteBackupDirectory;
            set
            {
                if (value == remoteBackupDirectory)
                    return;

                remoteBackupDirectory = value;
                propChanged(nameof(RemoteBackupDirectory));
            }
        }
        public bool UseShadowCopy
        {
            get => useShadowCopy;
            set
            {
                if (value == useShadowCopy)
                    return;

                useShadowCopy = value;
                propChanged(nameof(UseShadowCopy));
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Načte třídu konfigurace z textu XML.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static Config FromXML(string text)
        {
            XmlSerializer ser = new XmlSerializer(typeof(Config));
            Config cfg;
            using (StringReader reader = new StringReader(text))
                cfg = ser.Deserialize(reader) as Config;
            cfg.UnsavedChanges = false;
            return cfg;
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
}
