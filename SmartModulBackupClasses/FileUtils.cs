using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Security.AccessControl;
using SmartModulBackupClasses;
using System.Diagnostics;

namespace SmartModulBackupClasses
{
    public static class FileUtils
    {
        /// <summary>
        /// Zkopíruje obsah složky path do složky destination (rekurzivně)
        /// </summary>
        /// <param name="path"></param>
        /// <param name="destination"></param>
        /// <param name="errorPaths"></param>
        public static bool CopyFolderContents(string path, string destination, List<string> errorPaths = null, Func<FileInfo, bool> filter = null)
        {
            if (!Directory.Exists(path))
                return false;

            bool alles_gute = true;

            if (!Directory.Exists(destination))
                Directory.CreateDirectory(destination);
            //Directory.SetAccessControl(destination, AccessControlSections.All);

            foreach (string p in Directory.GetFiles(path))
            {
                string target = Path.Combine(destination, Path.GetFileName(p));
                try
                {
                    if (filter == null || filter(new FileInfo(p)))
                    {
                        File.Copy(p, target, true);
                        File.SetAttributes(target, FileAttributes.Normal);
                    }
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
                    CopyFolderContents(p, target, errorPaths, filter);
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

        public static bool CopyFolder(string path, string destination, Func<FileInfo, bool> filter = null) => 
            CopyFolderContents(path, Path.Combine(destination, Path.GetFileName(path)), filter: filter);

        public static bool MoveFolderContentsOverride(string src_path, string destination, List<string> errorPaths = null)
        {
            Directory.CreateDirectory(destination);

            foreach(var dir in Directory.GetDirectories(src_path))
                MoveFolderContentsOverride(dir, Path.Combine(destination, Path.GetFileName(dir)), errorPaths);

            foreach(var file in Directory.GetFiles(src_path))
            {
                try
                {
                    string dest_file = Path.Combine(destination, Path.GetFileName(file));
                    if (File.Exists(dest_file))
                        File.Delete(dest_file);
                    File.Move(file, dest_file);
                }
                catch
                {
                    errorPaths.Add(file);
                }
            }

            return !errorPaths.Any();
        }

        /// <summary>
        /// Zkopíruje soubory ze zdroje do cíle, které mají vyšší datum poslední změny.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="destination"></param>
        /// <param name="errorPaths"></param>
        /// <returns></returns>
        public static bool CopyFolderContentsDiff(string path, string destination, bool delete = false, List<string> errorPaths = null)
        {
            if (!Directory.Exists(path))
                return false;

            Directory.CreateDirectory(destination);

            bool alles_gute = true;
            foreach(var file in Directory.GetFiles(path))
            {
                var dest_file = Path.Combine(destination, Path.GetFileName(file));

                try
                {
                    if (!File.Exists(dest_file) || File.GetLastWriteTimeUtc(file) > File.GetLastWriteTimeUtc(dest_file).AddSeconds(1))
                    {
                        //Console.WriteLine($"{file} >> {dest_file}");
                        File.Copy(file, dest_file, true);
                    }
                }
                catch (Exception ex)
                {
                    errorPaths?.Add(file);
                    alles_gute = false;
                }
            }

            foreach(var folder in Directory.GetDirectories(path))
            {
                var dest_folder = Path.Combine(destination, Path.GetFileName(folder));

                alles_gute = CopyFolderContentsDiff(folder, dest_folder, delete, errorPaths) && alles_gute;
            }

            return alles_gute;
        }

        public static bool CopyFolderDiff(string path, string destination, bool delete = false, List<string> errorPaths = null)
            => CopyFolderContentsDiff(path, Path.Combine(destination, Path.GetFileName(path)), delete, errorPaths);

        /// <summary>
        /// Chytrý odstraňovač složek a jejich obsahu.
        /// </summary>
        /// <param name="path">Cesta k složce pro odstranění.</param>
        /// <param name="helpAttributes">Pokud true, automaticky se před ostraněním položky nastaví atributy tak, aby nám v tom nebránily.</param>
        /// <param name="exceptionBehavior">Co dělat, když se nějakou položku nepodaří odstranit</param>
        /// <param name="log">Jestli zapisovat výjimky do logu</param>
        /// <returns>Zdali se úspěšně odstranily všechny položky.</returns>
        public static bool DeleteFolder(string path, bool helpAttributes = true,
            ItemExceptionBehavior exceptionBehavior = ItemExceptionBehavior.Continue,
            bool log = false, bool deleteSelf = true)
        {
            bool success = true;
            foreach(var file in Directory.GetFiles(path))
            {
                try
                {
                    if (helpAttributes)
                        File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                }
                catch (Exception e)
                {
                    if (log)
                        SmbLog.Error($"Nepodařilo se odstranit soubor {file}", e, LogCategory.Files);


                    if (exceptionBehavior == ItemExceptionBehavior.StopOnException)
                        return false;
                    else if (exceptionBehavior == ItemExceptionBehavior.ThrowException)
                        throw e;
                    else if (exceptionBehavior == ItemExceptionBehavior.Continue)
                        success = false;
                    else
                        throw new NotImplementedException();
                }
            }

            foreach(var folder in Directory.GetDirectories(path))
            {
                try
                {
                    if (helpAttributes)
                        File.SetAttributes(folder, FileAttributes.Normal);
                    DeleteFolder(folder, helpAttributes, exceptionBehavior, log);
                }
                catch (Exception e)
                {
                    if (log)
                        SmbLog.Error($"Nepodařilo se odstranit složku {folder}", e, LogCategory.Files);
                    if (exceptionBehavior == ItemExceptionBehavior.StopOnException)
                        return false;
                    else if (exceptionBehavior == ItemExceptionBehavior.ThrowException)
                        throw e;
                    else if (exceptionBehavior == ItemExceptionBehavior.Continue)
                        success = false;
                    else
                        throw new NotImplementedException();
                }
            }

            if (deleteSelf)
                try
                {
                    if (helpAttributes)
                        File.SetAttributes(path, FileAttributes.Normal);
                    Directory.Delete(path, false);
                }
                catch (Exception e)
                {
                    if (log)
                        SmbLog.Error($"Nepodařilo se odstranit složku {path}", e, LogCategory.Files);

                    if (exceptionBehavior == ItemExceptionBehavior.StopOnException)
                        return false;
                    else if (exceptionBehavior == ItemExceptionBehavior.ThrowException)
                        throw e;
                    else if (exceptionBehavior == ItemExceptionBehavior.Continue)
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
