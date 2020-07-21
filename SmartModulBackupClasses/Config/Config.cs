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
    /// <summary>
    /// Info o konfiguraci klientské aplikace
    /// </summary>
    public class Config : INotifyPropertyChanged
    {
        private bool unsavedChanges;

        public bool FirstGuiRun { get; set; } = true;

        /// <summary>
        /// Jestli došlo v tomto objektu nebo v některém z jeho potomků ke změně vlastnosti.
        /// </summary>
        [XmlIgnore]
        public bool UnsavedChanges
        {
            get => unsavedChanges;
            set
            {
                if (value == unsavedChanges)
                    return;

                unsavedChanges = value;

                //pokud to nastavujem na false, chceme to nastavit i u potomků
                if (value == false)
                {
                    if (Connection != null)
                        connection.UnsavedChanges = value;
                    if (SFTP != null)
                        SFTP.UnsavedChanges = value;
                    if (WebCfg != null)
                        WebCfg.UnsavedChanges = value;
                }

                propChanged(nameof(UnsavedChanges));
            }
        }

        private bool useShadowCopy = false;
        private string remoteBackupDirectory = "Backups";
        private string localBackupDirectory = null;
        private DatabaseConfig connection = new DatabaseConfig();
        private SftpConfig sFTP = new SftpConfig();
        private WebConfig webCfg = new WebConfig();
        private EmailConfig emailConfig = new EmailConfig();

        public Config()
        {
            PropertyChanged += Config_PropertyChanged;
        }

        /// <summary>
        /// Zpracovává PropertyChanged událost tohoto objektu a jeho potomků (DatabaseConfig Conneciton,
        /// SftpConfig SFTP, WebConfig WebCfg)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Config_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(UnsavedChanges))
                UnsavedChanges = true;
        }

        private void propChanged(string prop)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        /// <summary>
        /// Info o připojení k SQL databázi.
        /// </summary>
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

        /// <summary>
        /// Info o připojení k SFTP serveru.
        /// </summary>
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

        /// <summary>
        /// Info o připojení k webovému API.
        /// </summary>
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

        public EmailConfig EmailConfig
        {
            get => emailConfig;
            set
            {
                if (value == emailConfig)
                    return;

                if (emailConfig != null)
                    emailConfig.PropertyChanged -= Config_PropertyChanged;

                if (value != null)
                    value.PropertyChanged += Config_PropertyChanged;

                emailConfig = value;
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
        //public string RemoteBackupDirectory
        //{
        //    get => remoteBackupDirectory;
        //    set
        //    {
        //        if (value == remoteBackupDirectory)
        //            return;

        //        remoteBackupDirectory = value;
        //        propChanged(nameof(RemoteBackupDirectory));
        //    }
        //}
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

        public LoggingConfig Logging { get; set; } = null;

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

        public void Loaded()
        {
            //inicializace LoggingConfig
            if (Logging == null)
            {
                Logging = new LoggingConfig();
                foreach(var l in Enum.GetValues(typeof(LogCategory)))
                {
                    Logging.Categories.Add(
                        new ConfigureLogCategory()
                        {
                            Category = (LogCategory)l,
                            LogLevel = NLog.LogLevel.Info.Name
                        }
                    );
                }

                Logging.Targets.EventLogTargets.Add(
                    new EventLogTarget() 
                    {
                        LogLevel = NLog.LogLevel.Trace.Name,
                        UsedByGui = false,
                        UsedByService = true
                    }
                );

                Logging.Targets.FileLogTargets.Add(
                    new FileLogTarget()
                    {
                        LogLevel = NLog.LogLevel.FromOrdinal(SmbLog.DEF_LOG_LEVEL_ORDINAL).Name,
                        UsedByGui = true,
                        UsedByService = false,
                        FileName = "log.txt"
                    }
                );
            }
        }
    }
}
