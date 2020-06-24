using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartModulBackupClasses
{
    public static class SMB_Log
    {
        public static event Action<LogArgs> OnLog;

        public static void LogEx(Exception ex, string addedMsg = null, int level = 0, LogType type = LogType.Error)
        {
            StringBuilder exStr = new StringBuilder();
            exStr.AppendLine($"Došlo k výjimce typu {ex.GetType().Name};");
            if (addedMsg != null)
                exStr.AppendLine(addedMsg);
            var trace = new StackTrace(ex, true).GetFrames().First(f => !f.GetMethod().DeclaringType.Namespace.StartsWith("System"));
            var meth = trace.GetMethod();
            exStr.AppendLine($"Metoda: {meth.DeclaringType.FullName + "." + meth.Name + "()"}, číslo řádku: {trace.GetFileLineNumber()}");
            exStr.AppendLine($"Výjimčí blekot: {ex.Message}");
            OnLog.Invoke(new LogArgs()
            {
                Message = exStr.ToString(),
                Level = level,
                Type = type
            });
        }

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
