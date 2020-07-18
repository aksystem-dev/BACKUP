using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SmartModulBackupClasses
{
    public enum SmbAssembly { Service, Gui }

    /// <summary>
    /// Společná třída využívaná službou i GUI pro zapisování věcí, které se v aplikaci dějou.
    /// </summary>
    public static class SmbLog
    {
        /// <summary>
        /// Zprávy s měnší ordinal než toto se nezapíšou.
        /// </summary>
        public const int MIN_LOG_LEVEL_ORDINAL = 0;

        /// <summary>
        /// ordinal pro neznámé kategorie
        /// </summary>
        public const int DEF_LOG_LEVEL_ORDINAL = 2;

        const string LAYOUT_EVENT_LOG = "${longdate} ${message}\n${exception:format=type,message,stackTrace:innerFormat=type,message}";
        const string LAYOUT_FILE_LOG = "${longdate} ${message}\n${exception:format=type,message,stackTrace:innerFormat=type,message}";


        public static bool IsConfigured { get; private set; } = false;
        private static NLog.Logger _nLog;
        private static LoggingConfig _config;
        private static Dictionary<LogCategory, NLog.LogLevel> _catLevels = new Dictionary<LogCategory, NLog.LogLevel>();

        /// <summary>
        /// Nastaví SmbLogger podle daného LoggingConfigu.
        /// </summary>
        /// <param name="config"></param>
        public static void Configure(LoggingConfig config, SmbAssembly assembly)
        {
            _config = config;

            var nLogConfig = new NLog.Config.LoggingConfiguration();

            //přidat do nLogConfigu pravidlo pro všechny EventLogTargety v configu
            int ind = 0;
            foreach(var target in _config.Targets.EventLogTargets)
            {
                try
                {
                    if ((assembly == SmbAssembly.Gui && target.UsedByGui) || (assembly == SmbAssembly.Service && target.UsedByService))
                    {
                        var nLogTarget = new NLog.Targets.EventLogTarget($"target_eventLog{ind}");
                        nLogTarget.Log = "SmartModulBackupLog";
                        nLogTarget.Layout = LAYOUT_EVENT_LOG;
                        nLogConfig.AddRule(NLog.LogLevel.FromString(target.LogLevel), NLog.LogLevel.Fatal, nLogTarget);
                    }
                }
                catch { }

                ind++;
            }

            //přidat do nLogConfigu pravidlo pro všechny FileLogTargety v configu
            ind = 0;
            foreach(var target in _config.Targets.FileLogTargets)
            {
                try
                {
                    if ((assembly == SmbAssembly.Gui && target.UsedByGui) || (assembly == SmbAssembly.Service && target.UsedByService))
                    {
                        var nLogTarget = new NLog.Targets.FileTarget($"target_fileLog{ind}");
                        nLogTarget.FileName = target.FileName;
                        nLogTarget.Layout = LAYOUT_FILE_LOG;
                        nLogConfig.AddRule(NLog.LogLevel.FromString(target.LogLevel), NLog.LogLevel.Fatal, nLogTarget);
                    }
                }
                catch { }

                ind++;
            }

            //nastavit NLog konfiguraci
            NLog.LogManager.Configuration = nLogConfig;

            _nLog = NLog.LogManager.GetLogger(Assembly.GetEntryAssembly().GetName().Name);


            _catLevels.Clear();
            foreach (var cat in _config.Categories)
                try
                {
                    _catLevels[cat.Category] = NLog.LogLevel.FromString(cat.LogLevel);
                }
                catch { }

            IsConfigured = true;
        }

        /// <summary>
        /// Zapíše danou zprávu.
        /// </summary>
        /// <param name="level"></param>
        /// <param name="message"></param>
        /// <param name="exception"></param>
        /// <param name="category"></param>
        public static void Log(NLog.LogLevel level, string message, Exception exception = null, LogCategory category = LogCategory.Default)
        {
            try
            {
                if (level.Ordinal < MIN_LOG_LEVEL_ORDINAL)
                    return;
                //pokud neznáme tuto kategorii a ordinal je menší než defaultní
                else if (!_catLevels.ContainsKey(category))
                {
                    if (level.Ordinal < DEF_LOG_LEVEL_ORDINAL)
                        return;
                }
                //pokud ordinal je menší než ordinal pro tuto kategorii
                else if (level.Ordinal < _catLevels[category].Ordinal)
                    return;

                if (exception != null)
                    _nLog.Log(level, exception, category.ToString() + ": " + message);
                else
                    _nLog.Log(level, category.ToString() + ": " + message);
            }
            catch { }
        }

        public static void Trace(string message, Exception exception = null, LogCategory category = LogCategory.Default)
            => Log(NLog.LogLevel.Trace, message, exception, category);

        public static void TraceMethodEnter()
            => Log(NLog.LogLevel.Trace, $"Vstoupeno do metody {new StackTrace().GetFrame(1).GetMethod().Name}");

        public static void Debug(string message, Exception exception = null, LogCategory category = LogCategory.Default)
            => Log(NLog.LogLevel.Debug, message, exception, category);

        public static void Info(string message, Exception exception = null, LogCategory category = LogCategory.Default)
            => Log(NLog.LogLevel.Info, message, exception, category);

        public static void Warn(string message, Exception exception = null, LogCategory category = LogCategory.Default)
            => Log(NLog.LogLevel.Warn, message, exception, category);

        public static void Error(string message, Exception exception = null, LogCategory category = LogCategory.Default)
            => Log(NLog.LogLevel.Error, message, exception, category);

        public static void Fatal(string message, Exception exception = null, LogCategory category = LogCategory.Default)
            => Log(NLog.LogLevel.Fatal, message, exception, category);
    }
}
