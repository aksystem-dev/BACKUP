using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Security.AccessControl;
using SmartModulBackupClasses;

namespace smart_modul_BACKUP_service
{
    public static class FileUtils
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
            //Directory.SetAccessControl(destination, AccessControlSections.All);

            foreach (string p in Directory.GetFiles(path))
            {
                string target = Path.Combine(destination, Path.GetFileName(p));
                try
                {
                    File.Copy(p, target, true);
                    File.SetAttributes(target, FileAttributes.Normal);
                    //File.SetLastWriteTime(target, DateTime.Now);
                }
                catch (Exception ex)
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
                    //Directory.SetLastWriteTime(target, DateTime.Now);
                }
                catch (Exception ex)
                {
                    errorPaths?.Add(Path.GetFullPath(p));
                    alles_gute = false;
                }
            }

            return alles_gute;
        }

        public static void CopyFolder(string path, string destination) => 
            CopyFolderContents(path, Path.Combine(destination, Path.GetFileName(path)));

        /// <summary>
        /// Chytrý odstraňovač složek a jejich obsahu.
        /// </summary>
        /// <param name="path">Cesta k složce pro odstranění.</param>
        /// <param name="help_attributes">Pokud true, automaticky se před ostraněním položky nastaví atributy tak, aby nám v tom nebránily.</param>
        /// <param name="exception_behavior">Co dělat, když se nějakou položku nepodaří odstranit</param>
        /// <param name="log">Jestli zapisovat výjimky do logu</param>
        /// <returns>Zdali se úspěšně odstranily všechny položky.</returns>
        public static bool DeleteFolder(string path, bool help_attributes = true,
            ItemExceptionBehavior exception_behavior = ItemExceptionBehavior.Continue,
            bool log = false)
        {
            bool success = true;
            foreach(var file in Directory.GetFiles(path))
            {
                try
                {
                    if (help_attributes)
                        File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                }
                catch (Exception e)
                {
                    if (log)
                        SMB_Log.LogEx(e);


                    if (exception_behavior == ItemExceptionBehavior.StopOnException)
                        return false;
                    else if (exception_behavior == ItemExceptionBehavior.ThrowException)
                        throw e;
                    else if (exception_behavior == ItemExceptionBehavior.Continue)
                        success = false;
                    else
                        throw new NotImplementedException();
                }
            }

            foreach(var folder in Directory.GetDirectories(path))
            {
                try
                {
                    if (help_attributes)
                        File.SetAttributes(folder, FileAttributes.Normal);
                    DeleteFolder(folder, help_attributes, exception_behavior, log);
                }
                catch (Exception e)
                {
                    if (log)
                        SMB_Log.LogEx(e);
                    if (exception_behavior == ItemExceptionBehavior.StopOnException)
                        return false;
                    else if (exception_behavior == ItemExceptionBehavior.ThrowException)
                        throw e;
                    else if (exception_behavior == ItemExceptionBehavior.Continue)
                        success = false;
                    else
                        throw new NotImplementedException();
                }
            }

            try
            {
                if (help_attributes)
                    File.SetAttributes(path, FileAttributes.Normal);
                Directory.Delete(path, false);
            }
            catch (Exception e)
            {
                if (log)
                    SMB_Log.LogEx(e);


                if (exception_behavior == ItemExceptionBehavior.StopOnException)
                    return false;
                else if (exception_behavior == ItemExceptionBehavior.ThrowException)
                    throw e;
                else if (exception_behavior == ItemExceptionBehavior.Continue)
                    success = false;
                else
                    throw new NotImplementedException();
            }

            return success;
        }

        public static long GetDirSize(string path)
        {
            long total = 0;
            foreach(var subpath in Directory.GetFileSystemEntries(path))
            {
                if (File.Exists(subpath))
                    total += new FileInfo(subpath).Length;
                else
                    total += GetDirSize(subpath);
            }
            return total;
        }
    }

    public enum ItemExceptionBehavior
    {
        /// <summary>
        /// Pokud dojde k výjimce, položka se přeskočí a pokračuje se.
        /// </summary>
        Continue, 

        /// <summary>
        /// Pokud dojde k výjimce, metoda jí vyhodí.
        /// </summary>
        ThrowException, 

        /// <summary>
        /// Pokud dojde k výjimce, metoda přestane vyhodnocovat další položky a vrátí.
        /// </summary>
        StopOnException
    }
}
