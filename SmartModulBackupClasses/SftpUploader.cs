using Renci.SshNet;
using Renci.SshNet.Common;
using Renci.SshNet.Sftp;
using SmartModulBackupClasses;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartModulBackupClasses
{
    /// <summary>
    /// Obaluje SftpClient, poskytuje další funkce
    /// </summary>
    public class SftpUploader : IDisposable
    {
        private void logTrace(string message)
            => SmbLog.Trace(message, null, LogCategory.SFTP);

        public SftpClient client { get; private set; }

        public bool IsConnected => client.IsConnected;

        public SftpUploader(string host, int port, string username, string password)
        {
            client = new SftpClient(host, port, username, password);
        }

        /// <summary>
        /// Pokusí se připojit a vrátí, zdali to bylo úspěšné.
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public bool TryConnect(int timeout = 500)
        {
            try
            {
                var rememberTimeout = client.OperationTimeout;
                client.OperationTimeout = TimeSpan.FromMilliseconds(timeout);
                client.Connect();
                client.OperationTimeout = rememberTimeout;
                return true;
            }
            catch { return false; }
        }

        public async Task<bool> TryConnectAsync(int timeout) => await Task.Run(() => TryConnect(timeout));

        /// <summary>
        /// Připojí se.
        /// </summary>
        public void Connect()
        {
            if (!client.IsConnected)
                client.Connect();

        }

        /// <summary>
        /// Odpojí se.
        /// </summary>
        /// <param name="throwException"></param>
        /// <returns></returns>
        public bool Disconnect(bool throwException = true)
        {
            try
            {
                if (client.IsConnected)
                    client.Disconnect();

                return true;
            }
            catch (Exception ex)
            {
                if (throwException)
                    throw ex;
                return false;
            }
        }

        public Dictionary<string, SftpFile> ListDir(SftpFile dir, bool recursive)
        {
            if (!dir.IsDirectory)
                throw new ArgumentException("parametr dir musí být složka");

            var to_return = new Dictionary<string, SftpFile>();

            //projít obsah vzdálené složky
            foreach (var entry in client.ListDirectory(dir.FullName))
            {
                //přidat normální soubory
                if (entry.IsRegularFile)
                    to_return.Add(entry.FullName.NormalizePath(), entry);
                //přidat adresáře
                else if (entry.IsDirectory)
                {
                    if (entry.Name == "." || entry.Name == "..") //tyto názvy neberem
                        continue;

                    to_return.Add(entry.FullName.NormalizePath(), entry);

                    //pokud recursive, přidat rekurzivně obsah adresáře
                    if (recursive)
                        foreach (var sub_entry in ListDir(entry.FullName, true))
                            to_return.Add(sub_entry.Key, sub_entry.Value);
                }
            }

            return to_return;
        }

        public Dictionary<string, SftpFile> ListDir(string dir, bool recursive)
        {
            //pokud na serveru složka dir neexistuje, vrátit prázdný slovník
            if (!client.Exists(dir))
                return new Dictionary<string, SftpFile>();

            var entry = client.Get(dir);
            if (!entry.IsDirectory)
                return new Dictionary<string, SftpFile>();

            return ListDir(entry, recursive);
        }

        /// <summary>
        /// Nahraje soubor (přepíše ho, pokud již existuje) (vytvoří pro něj složku, pokud neexistuje)
        /// </summary>
        /// <param name="localSource"></param>
        /// <param name="remoteDestination"></param>
        public long UploadFile(string localSource, string remoteDestination, Action<ulong> uploadCallback = null)
        {
            logTrace($"UploadFile({localSource}, {remoteDestination})");

            remoteDestination = remoteDestination.NormalizePath();
            CreateDirectory(remoteDestination.PathMoveUp());

            using (var stream = File.OpenRead(localSource))
            {
                client.UploadFile(stream, remoteDestination, true, uploadCallback);
                return stream.Length;
            }
        }

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="localSource"></param>
        ///// <param name="remoteDestination"></param>
        //public void UploadFileDiff(string localSource, string remoteDestination)
        //{
        //    remoteDestination = remoteDestination.NormalizePath();
        //    CreateDirectory(remoteDestination.PathMoveUp());

        //    DateTime localFileLastWriteTimeUtc = File.GetLastWriteTimeUtc(localSource);
        //    bool doIt = false;
        //    if (!client.Exists(remoteDestination))
        //        doIt = true;
        //    else
        //    {
        //        DateTime remoteFileLastWriteTimeUtc = client.GetLastWriteTime(remoteDestination).ToUniversalTime();
        //        if (localFileLastWriteTimeUtc > remoteFileLastWriteTimeUtc.AddSeconds(1))
        //            doIt = true;
        //    }

        //    //soubor nahrajeme pouze, pokud na serveru není, nebo je datum poslední změny souboru na serveru minimálně o vteřinu dřív, než datum poslední změny lokálního souboru
        //    if (doIt)
        //    {
        //        Console.WriteLine($"local {localSource} >> remote {remoteDestination}");
        //        using (var stream = File.OpenRead(localSource))
        //            client.UploadFile(stream, remoteDestination, true);
        //        //client.SetLastWriteTimeUtc(remoteDestination, localLastWriteTime);
        //        var att = client.GetAttributes(remoteDestination);
        //        att.LastWriteTime = localFileLastWriteTimeUtc.ToLocalTime();
        //        client.SetAttributes(remoteDestination, att);
        //    }
        //}

        /// <summary>
        /// Nahraje adresář
        /// </summary>
        /// <param name="localSource"></param>
        /// <param name="remoteDestination"></param>
        public long UploadDirectory(string localSource, string remoteDestination,
            FolderUploadBehavior behavior, Func<FileInfo, bool> filter = null,
            Action<ulong> uploadCallback = null)
        {
            //ověřit, že existuje lokální složka
            if (!Directory.Exists(localSource))
                throw new DirectoryNotFoundException("Lokální složka neexistuje.");

            //pokud dojde k problému, plesknem ho zde
            List<Exception> problems = new List<Exception>();

            //získati info o vzdálené složce
            var remdir = GetDirectory(remoteDestination);

            //pakliže neexistuje, vytvoříme jí
            if (remdir == null) 
                CreateDirectory(remoteDestination);
            //pakliže existuje, ale chování je nastaveno tak, že jí máme nahradit, tak jí odstraníme její vnitřek
            else if (behavior == FolderUploadBehavior.ReplaceWhole) 
            {
                if (remdir != null)
                    DeleteDirectory(remdir.FullName, true);
                behavior = FolderUploadBehavior.AddKeep;
            }

            //množství bytů, které jsme již nahráli
            long uploaded = 0;

            //projít všechny soubory v lokální složce
            foreach(var path in Directory.GetFileSystemEntries(localSource))
            {
                try
                {
                    var fname = Path.GetFileName(path);
                    var remote_path = Path.Combine(remoteDestination, fname).NormalizePath();

                    //je-li to složka
                    if (Directory.Exists(path))
                    {
                        //sub_callback je jako uploadCallback, akorát se k parametru přičte již nahrané množství dat (uploaded)
                        var sub_callback = uploadCallback != null ? new Action<ulong>(ul => uploadCallback((ulong)uploaded + ul)) : null;

                        //rekurzivně zavolat tuto funkci
                        uploaded += UploadDirectory(path, remote_path, behavior, filter, sub_callback);
                    }
                    //je-li to soubor
                    else if (File.Exists(path) && (filter == null || filter(new FileInfo(path))))
                    {
                        //info o vzdáleném souboru
                        var remote_info = client.Exists(remote_path) ? client.Get(remote_path) : null;

                        //pokud vzdálený soubor neexistuje nebo [pokud existuje a máme ho přepsat]
                        if (remote_info == null || (remote_info.IsRegularFile && behavior == FolderUploadBehavior.AddOverwrite))
                        {
                            var sub_callback = uploadCallback != null ? new Action<ulong>(ul => uploadCallback((ulong)uploaded + ul)) : null;
                            uploaded += UploadFile(path, remote_path, sub_callback);
                        }
                    }
                }
                catch (Exception ex)
                {
                    problems.Add(ex);
                }
            }

            if (problems.Any())
                throw new AggregateException(problems);

            return uploaded;
        }

        /// <summary>
        /// Nahraje změněné sourobry do adresáře
        /// </summary>
        /// <param name="localSource"></param>
        /// <param name="remoteDestination"></param>
        public DiffDirUploadResult UploadDirectoryDiff(string localSource, string remoteDestination, bool delete = false,
            Action<ulong> uploadCallback = null)
        {
            logTrace($"UploadDirectoryDiff({localSource}, {remoteDestination})");

            //ověřit, že existuje lokální složka
            if (!Directory.Exists(localSource))
                throw new DirectoryNotFoundException("Lokální složka neexistuje.");

            remoteDestination = remoteDestination.NormalizePath();
            //localSource = localSource.NormalizePath();

            //pokud dojde k problému, plesknem ho zde
            List<Exception> problems = new List<Exception>();

            //získati info o vzdálené složce
            var remdir = GetDirectory(remoteDestination);

            //pakliže neexistuje, vytvoříme jí
            if (remdir == null)
            {
                CreateDirectory(remoteDestination);
                remdir = GetDirectory(remoteDestination);
            }

            //získat seznam stávajících souborů v cílové složce na serveru
            var remote_contents = ListDir(remdir, false);

            //zde budeme ukládat cesty ke vzdáleným souborům, které jsme spárovali s lokálními
            var seen_dest_paths = delete ? new HashSet<string>() : null;

            //SEM SE VRÁTIT!!!

            //množství bytů, které jsme již nahráli (nebo přeskočili)
            long uploaded = 0;
            long uploaded_with_skipping = 0;

            //projít všechny soubory v lokální složce
            foreach (var pair in FileUtils.ListDir(localSource, false))
            {
                var local_path = pair.Key;
                var local_info = pair.Value;

                try
                {
                    var fname = Path.GetFileName(local_path);
                    var remote_path = Path.Combine(remoteDestination, fname).NormalizePath();

                    //získat info o odpovídajícím vzdáleném souboru
                    SftpFile remote_file = null;
                    if (remote_contents.ContainsKey(remote_path))
                    {
                        remote_file = remote_contents[remote_path];

                        //zapsat tento soubor do seznamu viděných cílových
                        if (delete)
                            seen_dest_paths.Add(remote_path);
                    }

                    if (local_info is DirectoryInfo)
                    {
                        //pokud nám překáží soubor se stejným názvem a delete == true, odstranit ho
                        if (remote_file?.IsRegularFile == true && delete)
                            try
                            {
                                DeleteFile(remote_path);
                            }
                            catch (Exception ex)
                            {
                                SmbLog.Error($"Problém při odstraňování překážejícího souboru {remote_path} při UploadDirectoryDiff", ex, LogCategory.SFTP);
                            }

                        //rekurzivně zavolat tuto metodu pro porychtování podadresáře
                        var subresult = UploadDirectoryDiff(local_path, remote_path, delete, uploadCallback);
                        uploaded += subresult.uploadedSize;
                        uploaded_with_skipping += subresult.uploadedWithSkippingSize;
                    }
                    else if (local_info is FileInfo f_info)
                    {
                        bool do_upload = false; //zde uložíme, jestli chceme cílový soubor přepsat zdrojovým

                        //pokud nám překáží adresář se stejným názvem a delete == true, odstranit ho
                        if (remote_file?.IsDirectory == true && delete)
                            try
                            {
                                do_upload = true;
                                DeleteDirectory(remote_path);
                            }
                            catch (Exception ex)
                            {
                                SmbLog.Error($"Problém při odstraňování překážejícího adresáře {remote_path} při UploadDirectoryDiff", ex, LogCategory.SFTP);
                            }

                        //pokud cílový soubor neexistuje
                        //nebo pokud má zdrojový soubor datum změny > cílový soubor + 1s, do_upload musí být true
                        if (remote_file == null
                            || local_info.LastWriteTimeUtc > remote_file.LastWriteTimeUtc.AddSeconds(1))
                            do_upload = true;

                        if (do_upload)
                        {
                            var sub_callback = uploadCallback != null ?
                                new Action<ulong>(ul => uploadCallback((ulong)uploaded_with_skipping + ul)) : null;
                            long len = UploadFile(local_path, remote_path, sub_callback);
                            uploaded += len;
                            uploaded_with_skipping += len;
                        }
                        else
                            uploaded_with_skipping += f_info.Length;
                    }
                }
                catch (Exception ex)
                {
                    SmbLog.Error($"Nepodařilo se nahrát soubor/složku {local_path} na SFTP server.", ex, LogCategory.SFTP);
                    problems.Add(ex);
                }
            }

            //projít soubory ve vzdálené složce
            if (delete)
                foreach (var pair in remote_contents)
                {
                    //přeskočit soubory, pro které jsme našli odpovídající zdrojové soubory
                    if (seen_dest_paths.Contains(pair.Key))
                        continue;

                    if (pair.Value.IsRegularFile)
                        try
                        {
                            DeleteFile(pair.Key);
                        }
                        catch (Exception ex)
                        {
                            problems.Add(ex);
                            SmbLog.Error("Nepodařilo se odstranit soubor, který byl smazán (UploadDirectoryDiff)", ex, LogCategory.SFTP);
                        }
                    else if (pair.Value.IsDirectory)
                        try
                        {
                            DeleteDirectory(pair.Key);
                        }
                        catch (Exception ex)
                        {
                            problems.Add(ex);
                            SmbLog.Error("Nepodařilo se odstranit adresář, který byl smazán (UploadDirectoryDiff)", ex, LogCategory.SFTP);
                        }
                }

            if (problems.Any())
                throw new AggregateException(problems);

            return new DiffDirUploadResult(uploaded_with_skipping, uploaded);
        }



        public void DownloadFile(string remoteSource, string localDestionation, Action<ulong> downloadCallback = null)
        {
            remoteSource = remoteSource.NormalizePath();
            using (var writer = File.Create(localDestionation))
                client.DownloadFile(remoteSource, writer, downloadCallback);
        }

        /// <summary>
        /// Stáhne složku ze serveru.
        /// </summary>
        /// <param name="remoteSource"></param>
        /// <param name="localDestination"></param>
        /// <param name="behavior"></param>
        public void DownloadFolder(string remoteSource, string localDestination, FolderUploadBehavior behavior)
        {
            remoteSource = remoteSource.NormalizePath();

            if (!client.Exists(remoteSource))
                throw new SftpPathNotFoundException("Složka na serveru není.");

            var files = client.ListDirectory(remoteSource);

            //pokud lokální složka existuje a behavior == FolderUploadBehavior.ReplaceWhole, chceme lokální složku vyprázdnit,
            //než do ní začneme rvát soubory
            if (Directory.Exists(localDestination))
            {
                if (behavior == FolderUploadBehavior.ReplaceWhole)
                    Directory.Delete(localDestination, true);
                behavior = FolderUploadBehavior.AddOverwrite;
            }
            //pokud lokální složka neexistuje, vytvoříme jí
            else
                Directory.CreateDirectory(localDestination);

            //sem budeme ukládat problémy
            List<Exception> problems = new List<Exception>();

            foreach(var file in files)
            {
                //toto nejsou fyzické soubory, kašlemž na ně
                if (file.Name == "." || file.Name == "..")
                    continue;

                try
                {
                    //cesta k lokálnímu souboru
                    string localPath = Path.Combine(localDestination, Path.GetFileName(file.FullName));

                    if (file.IsDirectory)
                    {
                        //rekurzivní stažení podadresáře
                        DownloadFolder(file.FullName, localPath, behavior);
                    }
                    else if (file.IsRegularFile)
                    {
                        if (File.Exists(localPath) && behavior == FolderUploadBehavior.AddKeep)
                            continue;

                        //stažení souboru
                        DownloadFile(file.FullName, localPath);
                    }
                }
                catch (AggregateException ex)
                {
                    //došlo-li k více problémům, přidáme je na seznam všechny
                    problems.AddRange(ex.InnerExceptions);
                }
                catch (Exception ex)
                {
                    //došlo-li k problému, přidáme ho na seznam
                    problems.Add(ex);
                }
            }

            //pokud došlo k nějakému problému, vyhodíme AggregateException s nimi
            if (problems.Any())
                throw new AggregateException(problems);
        }

        /// <summary>
        /// Odstraní vzdálenou složku a její vnitřek.
        /// </summary>
        /// <param name="remoteDirectory"></param>
        /// <param name="onlyContents"></param>
        public void DeleteDirectory(string remoteDirectory, bool onlyContents = false)
        {
            remoteDirectory = remoteDirectory.NormalizePath();

            List<Exception> problems = new List<Exception>();

            var files = client.ListDirectory(remoteDirectory);
            foreach (var file in files)
            {
                //tyhle srandy se nevztahují k fyzickým souborům, tudíž je ignorujem
                if (file.Name == "." || file.Name == "..")
                    continue;

                try
                {
                    if (file.IsRegularFile)
                        file.Delete();
                    else if (file.IsDirectory)
                        DeleteDirectory(file.FullName);
                }
                catch (Exception ex)
                {
                    problems.Add(ex);
                }
            }

            if (!onlyContents)
                try
                {
                    client.Delete(remoteDirectory);
                }
                catch (Exception ex)
                {
                    problems.Add(ex);
                }

            if (problems.Count > 0)
                throw new AggregateException(problems);
        }

        /// <summary>
        /// Odstraní soubor
        /// </summary>
        /// <param name="remoteFile"></param>
        public void DeleteFile(string remoteFile)
        {
            //Logger.Log($"client.delete({remoteFile.FixPathForSFTP()})");
            client.Delete(remoteFile.NormalizePath());
        }


        /// <summary>
        /// Vytvoří složku
        /// </summary>
        /// <param name="remoteDestination"></param>
        public void CreateDirectory(string remoteDestination)
        {
            remoteDestination = remoteDestination.NormalizePath();

            string[] paths = remoteDestination.PathProgression();
            for (int i = 1; i < paths.Length; i++)
            {
                //pokud složka o úroveň výš neobsahuje složku, kterou chceme, aby obsahovala, musíme jí vytvořit
                if (!client.Exists(paths[i]))
                {
                    client.CreateDirectory(paths[i]);
                }
            }
        }

        public SftpFile GetFile(string path)
        {
            path = path.NormalizePath();

            if (!client.Exists(path))
                return null;

            var file = client.Get(path);
            return file.IsRegularFile ? file : null;
        }

        public SftpFile GetDirectory(string path)
        {
            path = path.NormalizePath();

            if (!client.Exists(path))
                return null;

            var file = client.Get(path);
            return file.IsDirectory ? file : null;
        }

        public long GetDirSize(string path)
        {
            path = path.NormalizePath();
            if (!client.Exists(path))
                return 0;

            var files = client.ListDirectory(path);

            long length = 0;
            foreach(var file in files)
            {
                if (file.Name == "." || file.Name == "..")
                    continue;

                if (file.IsDirectory)
                    length += GetDirSize(file.FullName);
                else if (file.IsRegularFile)
                    length += file.Length;
            }

            return length;
        }

        public void Dispose()
        {
            client.Dispose();
        }
    }

    public enum FolderUploadBehavior
    {
        /// <summary>
        /// Pokud na cílovém umístění již složka je, odstraní se, a poté se nahraje nová
        /// </summary>
        ReplaceWhole,

        /// <summary>
        /// Pokud na cílovém umístění již složka je, přidají se do ní soubory ve zdrojové složce, které tam nejsou,
        /// ale soubory, které tam již jsou, se nenahrají
        /// </summary>
        AddKeep,

        /// <summary>
        /// Pokud na cílovém umístění již složka je, přidají se do ní soubory ve zdrojové složce a případné
        /// konflikty se vyřeší tak, že se soubor na serveru přepíše nahraným souborem.
        /// </summary>
        AddOverwrite
    }

    public struct DiffDirUploadResult
    {
        public long uploadedWithSkippingSize;
        public long uploadedSize;

        public DiffDirUploadResult(long totalSize, long uploadedSize)
        {
            this.uploadedWithSkippingSize = totalSize;
            this.uploadedSize = uploadedSize;
        }
    }
}
