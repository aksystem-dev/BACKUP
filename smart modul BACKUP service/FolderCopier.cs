using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace smart_modul_BACKUP_service
{
    public static class FolderCopier
    {
        /// <summary>
        /// Zkopíruje obsah složky path do složky destination (rekurzivně)
        /// </summary>
        /// <param name="path"></param>
        /// <param name="destination"></param>
        /// <param name="errorPaths"></param>
        public static bool CopyFolderContents(string path, string destination, List<string> errorPaths = null)
        {
            bool alles_gute = true;

            if (!Directory.Exists(destination))
                Directory.CreateDirectory(destination);

            foreach (string p in Directory.GetFiles(path))
            {
                string target = Path.Combine(destination, Path.GetFileName(p));
                try
                {
                    File.Copy(p, target, true);
                    File.SetLastWriteTime(target, DateTime.Now);
                }
                catch
                {
                    errorPaths?.Add(Path.GetFullPath(p));
                    alles_gute = false;
                }
            }

            foreach(string p in Directory.GetDirectories(path))
            {
                string target = Path.Combine(destination, Path.GetFileName(p));
                try
                {
                    Directory.CreateDirectory(target);
                    CopyFolderContents(p, target);
                    Directory.SetLastWriteTime(target, DateTime.Now);
                }
                catch
                {
                    errorPaths?.Add(Path.GetFullPath(p));
                    alles_gute = false;
                }
            }

            return alles_gute;
        }

        public static void CopyFolder(string path, string destination) => 
            CopyFolderContents(path, Path.Combine(destination, Path.GetFileName(path)));
    }
}
