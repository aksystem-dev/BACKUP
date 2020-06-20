using SmartModulBackupClasses;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smart_modul_BACKUP_service
{
    /// <summary>
    /// Stará se o zapisování toho, co služba dělá
    /// </summary>
    public static class Logger
    {
        public static EventLog eventLog = null;
        private static int eventId = 0;

        public static void Log(string msg)
        {
            if (msg.Length < 32766)
                eventLog?.WriteEntry(msg, EventLogEntryType.Information, ++eventId);
        }

        public static void Error(string msg)
        {
            if (msg.Length < 32766)
                eventLog?.WriteEntry(msg, EventLogEntryType.Error, ++eventId);
        }

        public static void Ex(Exception ex)
        {
            SMB_Log.Log(ex);

            //StringBuilder exStr = new StringBuilder();
            //exStr.AppendLine($"Došlo k výjimce typu {ex.GetType().Name};");
            //var trace = new StackTrace(ex, true).GetFrame(0);
            //var meth = trace.GetMethod();
            //exStr.AppendLine($"Metoda: {meth.DeclaringType.FullName + "." + meth.Name + "()"}, číslo řádku: {trace.GetFileLineNumber()}");
            //exStr.AppendLine($"Výjimčí blekot: {ex.Message}");
            //eventLog?.WriteEntry(exStr.ToString(), EventLogEntryType.Error, ++eventId);
        }

        public static void Warn(string msg)
        {
            if (msg.Length < 32766)
                eventLog?.WriteEntry(msg, EventLogEntryType.Warning, ++eventId);
        }

        public static void Success(string msg)
        {
            if (msg.Length < 32766)
                eventLog?.WriteEntry(msg, EventLogEntryType.SuccessAudit, ++eventId);
        }

        public static void Failure(string msg)
        {
            if (msg.Length < 32766)
                eventLog?.WriteEntry(msg, EventLogEntryType.Warning, ++eventId);
        }
    }
}
