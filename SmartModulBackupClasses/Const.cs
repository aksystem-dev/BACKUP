using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartModulBackupClasses
{
    /// <summary>
    /// Nějaké konstantní hodnoty
    /// </summary>
    public static class Const
    {
        public const int UPDATE_GUI_SFTP_UPLOAD_MIN_MS_INTERVAL = 250;

        /// <summary>
        /// Složka, kam se ukládají pravidla
        /// </summary>
        public const string RULES_FOLDER = "Rules";

        /// <summary>
        /// název složky, kam se ukládají informace u zálohách <br />
        /// jak lokálně tak přes SFTP
        /// </summary>
        public const string BK_INFOS_FOLDER = "bkinfos";

        /// <summary>
        /// soubor na serveru obsahující info o daném PC
        /// </summary>
        public const string REMOTE_PC_INFO = "pcinfo.xml";

        /// <summary>
        /// název vzdálené složky, kam se ukládají samozné zálohy
        /// </summary>
        public const string REMOTE_DIR_BACKUPS = "Backups";


        public const string CFG_FILE = "config.xml";

        /// <summary>
        /// URL webové aplikace
        /// </summary>
        public const string SMB_URL = "https://localhost:5001/"; //TODO: toto nastavit podle aktuální adresy
    }
}
