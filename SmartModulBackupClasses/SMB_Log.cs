using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SmartModulBackupClasses
{
    /// <summary>
    /// Třída pro logování ve společném dll. Pokud se používá ze služby, na událost OnLog se pověsí
    /// zápis do EventLogu. Pokud se používá z GUI, na OnLog se pověsí zápis do souboru.
    /// </summary>
    [Obsolete("Použijte SmbLog.")]
    public static class SMB_Log
    {
        public static event Action<LogArgs> OnLog;

        /// <summary>
        /// Log výjimky
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="addedMsg"></param>
        /// <param name="level"></param>
        /// <param name="type"></param>
        public static void LogEx(Exception ex, string addedMsg = null, int level = 0, LogType type = LogType.Error)
        {
            StringBuilder exStr = new StringBuilder();
            exStr.AppendLine($"Došlo k výjimce typu {ex.GetType().Name};");
            if (addedMsg != null)
                exStr.AppendLine(addedMsg);
            var calling = new StackFrame(1);
            var trace = new StackTrace(ex, true).GetFrames().First(f => f.GetMethod().DeclaringType.Assembly == calling.GetMethod().DeclaringType.Assembly);
            var meth = trace.GetMethod();
            exStr.AppendLine($"Metoda: {meth.DeclaringType.FullName + "." + meth.Name + "()"}, číslo řádku volajícího log: {calling.GetFileLineNumber()}");
            exStr.AppendLine($"Výjimčí blekot: {ex.Message}");
            exStr.AppendLine($"Stack trace: {ex.StackTrace}");
            OnLog.Invoke(new LogArgs()
            {
                Message = exStr.ToString(),
                Level = level,
                Type = type
            });
        }

        /// <summary>
        /// Log nějakého textu
        /// </summary>
        /// <param name="message"></param>
        /// <param name="level"></param>
        /// <param name="type"></param>
        public static void Log(string message, int level = 0, LogType type = LogType.Info)
            => OnLog?.Invoke(new LogArgs()
            {
                Message = message,
                Level = level,
                Type = type
            });

        public static void Error(string message, int level = 0) => Log(message, level, LogType.Error);
        public static void Warn(string message, int level = 0) => Log(message, level, LogType.Warning);
    }

    public class LogArgs
    {
        public string Message;
        public int Level;
        public LogType Type;
    }

    public enum LogType { Info, Success, Error, Warning }
}
