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
        public bool FirstGuiRun { get; set; } = true;

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Zahlásí, že se změnily všechny vlastnosti, ať na to GUI zareaguje
        /// </summary>
        /// <param name="invoker"></param>
        public void AllPropertiesChanged(Action<Action> invoker = null)
        {
            if (PropertyChanged == null)
                return;

            invoker = invoker ?? new Action<Action>(a => a());
            var dgate = PropertyChanged;

            foreach (var prop in GetType().GetProperties())
            {
                if (prop.GetMethod != null && prop.GetMethod.IsPublic)
                    invoker(() => dgate(this, new PropertyChangedEventArgs(prop.Name)));
            }

            Connection.AllPropertiesChanged();
            SFTP.AllPropertiesChanged();
            WebCfg.AllPropertiesChanged();
            EmailConfig.AllPropertiesChanged();
        }

        /// <summary>
        /// Info o připojení k SQL databázi.
        /// </summary>
        public DatabaseConfig Connection { get; set; } = new DatabaseConfig();

        /// <summary>
        /// Info o připojení k SFTP serveru.
        /// </summary>
        public SftpConfig SFTP { get; set; } = new SftpConfig();

        /// <summary>
        /// Info o připojení k webovému API.
        /// </summary>
        public WebConfig WebCfg { get; set; } = new WebConfig();

        public EmailConfig EmailConfig { get; set; } = new EmailConfig();

        public string LocalBackupDirectory { get; set; } = null;

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
        public bool UseShadowCopy { get; set; } = false;

        public LoggingConfig Logging { get; set; } = null;

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
