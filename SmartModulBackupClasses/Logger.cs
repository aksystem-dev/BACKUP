using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartModulBackupClasses
{
    public static class SMB_Log
    {
        public static event Action<LogArgs> OnLog;

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
