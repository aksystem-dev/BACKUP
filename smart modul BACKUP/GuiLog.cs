using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smart_modul_BACKUP
{
    public static class GuiLog
    {
        public static string FileName = "guilog.txt";

        public static void Clear()
        {
            using (StreamWriter writer = new StreamWriter(FileName, false))
                writer.Write("");
        }

        public static void Log(string msg)
        {
            using (StreamWriter writer = new StreamWriter(FileName, true))
                writer.WriteLine(msg);
        }
    }
}
