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
        private static void logError(string error, Exception ex = null)
            => SmbLog.Error(error, ex, LogCategory.Files);

        private static void logTrace(string msg)
            => SmbLog.Trace(msg, null, LogCategory.Files);

        private static void logInfo(string msg)
             => SmbLog.Info(msg, null, LogCategory.Files);

        private static void logDebug(string msg)
             => SmbLog.Debug(msg, null, LogCategory.Files);

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

        /// <summary>
        /// Přesune složku do cílové destinace s tím, že v případném konfliktu názvů cílové objekty přepíše.
        /// </summary>
        /// <param name="src_path"></param>
        /// <param name="destination"></param>
        /// <param name="errorPaths"></param>
        /// <returns></returns>
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
        /// Vypíše všechny soubory a složky ve složce.
        /// </summary>
        /// <param name="dir">Cesta ke složce</param>
        /// <param name="recursive">Zdali zahrnout i obsah podsložek a podpodsložek a podpodpodsložek a tak dál</param>
        /// <returns></returns>
        public static Dictionary<string, FileSystemInfo> ListDir(string dir, bool recursive, bool normalize = false)
        {
            var to_return = new Dictionary<string, FileSystemInfo>();

            //projít všechny věci ve složce
            foreach(var path in (Directory.GetFiles(dir).Union(Directory.GetDirectories(dir))))
            {
                //nejprve zkusíme, jestli je to složka
                FileSystemInfo info = new DirectoryInfo(path);
                if (info.Exists) //je-li to složka
                {
                    to_return.Add(normalize ? path.NormalizePath() : path, info); //přidat to do slovníku

                    //pokud recursive == true, přidat do slovníku i obsah této složky
                    if (recursive)
                    {
                        foreach (var pair in ListDir(path, true, normalize))
                            to_return.Add(pair.Key, pair.Value);
                    }

                    continue;
                }

                //pokud se nejedná o složku, zkusíme, zda je to soubor
                info = new FileInfo(path);
                if (info.Exists) //je-li to soubor
                {
                    to_return.Add(normalize ? path.NormalizePath() : path, info); //přidat ho do slovníku
                    continue;
                }
            }

            return to_return;
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
            logTrace($"CopyFolderContentsDiff({path ?? "null"},{destination ?? "null"},{delete},{errorPaths?.ToString() ?? "null"})");

            if (!Directory.Exists(path))
                return false;

            //v případě, že složka destination neexistuje, vytvořit jí
            Directory.CreateDirectory(destination);

            //získat slovník s obsahem cílové složky
            var dest_dict = ListDir(destination, false, true);

            bool alles_gute = true; //jestli vše proběhlo bez chyb

            //zde budeme ukládat cílové cesty, které mají odpovídající zdrojovou cestu
            //nebudeme potřebovat, pokud delete == false
            HashSet<string> seen_destination_paths = delete ? new HashSet<string>() : null;

            foreach(var file in Directory.GetFiles(path)) //projít soubory ve zdrojovém adresáři
            {
                //zplácat dohromady název cílového souboru
                var dest_file = Path.Combine(destination, Path.GetFileName(file)).NormalizePath();

                logDebug($"CopyFolderContentsDiff: \"{file}\" >> \"{dest_file}\" ???");

                try
                {
                    //získat informace o cílovém souboru
                    FileInfo file_info = null;
                    if (dest_dict.ContainsKey(dest_file))
                    {
                        var info = dest_dict[dest_file];
                        if (info is FileInfo fi)
                            file_info = fi;
                        else if (info is DirectoryInfo di)
                        {
                            logDebug($"CopyFolderContentsDiff: konflikt názvu - v cílovém umístění existuje adresář s názvem zdrojového souboru");

                            //pokud v cílovém adresáři je složka se stejným názvem, odstranit ji pokud delete == true
                            if (delete)
                            {
                                DeleteFolder(di.FullName, true);
                            }
                        }

                        //přidat do seznamu cílových cest, které jsme zpracovali
                        if (delete)
                            seen_destination_paths.Add(dest_file);
                    }

                    if (file_info == null)
                        logDebug($"file_info is NULL");
                    else
                        logDebug($"file_info FullName=\"{file_info.FullName}\"");

                    //pokud cílový soubor neexistuje nebo je jeho poslední datum změny nižší než poslední datum změny
                    //zdrojového souboru, kopírovat zdrojový soubor do cílového souboru
                    if (file_info == null || File.GetLastWriteTimeUtc(file) > file_info.LastAccessTimeUtc.AddSeconds(1))
                    {
                        logDebug($"CopyFolderContentsDiff volá File.Copy(\"{file}\", \"{dest_file}\", true)");
                        File.Copy(file, dest_file, true);
                    }
                }
                catch (Exception ex)
                {
                    logError($"Došlo k problému při kopírování souboru {file} (CopyFolderContentsDiff)");
                    errorPaths?.Add(file);
                    alles_gute = false;
                }
            }

            foreach(var folder in Directory.GetDirectories(path))
            {
                var dest_folder = Path.Combine(destination, Path.GetFileName(folder)).NormalizePath();

                try
                {
                    //získat informace o cílovém adresáři
                    DirectoryInfo dir_info = null;
                    if (dest_dict.ContainsKey(dest_folder))
                    {
                        var info = dest_dict[dest_folder];
                        if (info is DirectoryInfo di)
                            dir_info = di;
                        else if (info is FileInfo fi)
                        {
                            logDebug($"CopyFolderContentsDiff: konflikt názvu - v cílovém umístění existuje soubor s názvem zdrojového adresáře");

                            //pokud existuje cílový soubor se stejným názvem, odstranit ji pokud delete == true
                            if (delete)
                                fi.Delete();
                        }

                        //přidat do seznamu cílových cest, které jsme zpracovali
                        if (delete)
                            seen_destination_paths.Add(dest_folder);
                    }

                    alles_gute = CopyFolderContentsDiff(folder, dest_folder, delete, errorPaths) && alles_gute;
                }
                catch(Exception ex)
                {
                    logError($"CopyFolderContentsDiff: Problém při kopírování složky {folder}");
                    errorPaths?.Add(folder);
                    alles_gute = false;
                }
            }

            if (delete)
            {
                //projít všechny soubory a adresáře v cílovém adresáři
                foreach(var dest_path in dest_dict.Keys)
                {
                    //přeskočit ty, co už jsme zpracovali
                    if (seen_destination_paths.Contains(dest_path))
                        continue;

                    //ostatní odstranit
                    var info = dest_dict[dest_path];
                    if (info is FileInfo)
                        try
                        {
                            SmbLog.Debug($"Odstraňuji soubor v cílovém adresáři {info.Name}");
                            info.Delete();
                        }
                        catch (Exception ex)
                        {
                            SmbLog.Error("Nepodařilo se odstranit soubor v cílovém adresáři (CopyFolderContentsDiff)", ex, LogCategory.Files);
                        }
                    else if (info is DirectoryInfo)
                    {
                        SmbLog.Debug($"Odstraňuji podadresář v cílovém adresáři {info.Name}");
                        DeleteFolder(info.FullName, true);
                    }
                }
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
