using SmartModulBackupClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace smart_modul_BACKUP
{
    public static class Bubble
    {
        public static void Show(string msg, ToolTipIcon icon = ToolTipIcon.Info, string title = "smart modul BACKUP", int timeout = 2000)
        {
            Manager.Get<System.Windows.Forms.NotifyIcon>().ShowBalloonTip(timeout, msg, title, icon);
        }
    }
}
