using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SmartModulBackupClasses
{
    /// <summary>
    /// Konfigurace logování.
    /// </summary>
    public class LoggingConfig
    {
        public List<ConfigureLogCategory> Categories { get; set; } = new List<ConfigureLogCategory>();
        public LogTargetCollection Targets { get; set; } = new LogTargetCollection();

        public void ConfigureNLog()
        {
            var nLog_cfg = new NLog.Config.LoggingConfiguration();

            var evLog = new NLog.Targets.EventLogTarget("target_eventLog");
            
        }
    }

    public class ConfigureLogCategory
    {
        [XmlAttribute(AttributeName = "category")]
        public LogCategory Category { get; set; }

        [XmlAttribute(AttributeName = "level")]
        public string LogLevel { get; set; }
    }

    public enum LogCategory
    {
        Default,
        BackupTask,
        RestoreTask,
        WebApi,
        Files,
        BackupInfoManager,
        BackupRuleLoader,
        SFTP,
        GUI,
        Service,
        BackupCleaner,
        BackupTimeline,
        FolderObserver,
        GuiServiceClient,
        OneGuiPerUser,
        ServiceHost,
        RuleScheduler,
        SQL,
        ShadowCopy,
        GuiSetup,
        GuiAvailableDbLoad,
        Emails
    }

    public class LogTargetCollection 
    {
        [XmlElement(ElementName = "EventLogTarget")]
        public List<EventLogTarget> EventLogTargets { get; set; } = new List<EventLogTarget>();

        [XmlElement(ElementName = "FileLogTarget")]
        public List<FileLogTarget> FileLogTargets { get; set; } = new List<FileLogTarget>();
    }

    public class LogTarget
    {
        [XmlAttribute(AttributeName = "level")]
        public string LogLevel { get; set; }

        [XmlAttribute(AttributeName = "usedByService")]
        public bool UsedByService { get; set; }

        [XmlAttribute(AttributeName = "usedByGui")]
        public bool UsedByGui { get; set; }

        [XmlAttribute(AttributeName = "clearOnStart")]
        public bool ClearOnStart { get; set; }
    }

    public class EventLogTarget : LogTarget { }

    public class FileLogTarget : LogTarget
    {
        [XmlAttribute(AttributeName = "filename")]
        public string FileName { get; set; }
    }
}
