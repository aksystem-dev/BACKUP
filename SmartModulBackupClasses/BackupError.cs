using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartModulBackupClasses
{
    /// <summary>
    /// Představuje chybu při záloze
    /// </summary>
    public class BackupError
    {
        public string Message { get; set; }
        public BackupErrorType ErrorType { get; set; }
        public string BackupSource { get; set; }

        public BackupError(string msg, BackupErrorType type, string refSource = null)
        {
            Message = msg;
            ErrorType = type;
            BackupSource = refSource;
        }

        public BackupError()
        {

        }
    }

    public enum BackupErrorType
    {
        SftpError,
        SqlError,
        ShadowCopyError,
        IOError,
        DefaultError
    }
}
