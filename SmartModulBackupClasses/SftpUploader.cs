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
        public SftpClient client { get; private set; }
        private int _users = 0;

        public bool IsConnected => client.IsConnected;

        public SftpUploader(string host, int port, string username, string password)
        {
            client = new SftpClient(host, port, username, password);
        }

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

        public void Connect()
        {
            if (!client.IsConnected)
                client.Connect();

            _users++;
        }

        public void Disconnect()
        {
            if (_users > 0)
                _users--;

            if (_users == 0 && client.IsConnected)
                client.Disconnect();
        }

        /// <summary>
        /// Nahraje soubor (přepíše ho, pokud již existuje) (vytvoří pro něj složku, pokud neexistuje)
        /// </summary>
        /// <param name="localSource"></param>
        /// <param name="remoteDestination"></param>
        public void UploadFile(string localSource, string remoteDestination)
        {
            CreateDirectory(remoteDestination.PathMoveUp());

            using (var stream = File.OpenRead(localSource))
                client.UploadFile(stream, remoteDestination.FixPathForSFTP(), true);
        }

        /// <summary>
        /// Nahraje adresář
        /// </summary>
        /// <param name="localSource"></param>
        /// <param name="remoteDestination"></param>
        public void UploadDirectory(string localSource, string remoteDestination, FolderUploadBehavior behavior)
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

            //projít všechny soubory v lokální složce
            foreach(var path in Directory.GetFileSystemEntries(localSource))
            {
                try
                {
                    var fname = Path.GetFileName(path);
                    var remote_path = Path.Combine(remoteDestination, fname).FixPathForSFTP();
                    if (Directory.Exists(path))
                    {
                        UploadDirectory(path, remote_path, behavior);
                    }
                    else if (File.Exists(path))
                    {
                        var remote_info = client.Exists(remote_path) ? client.Get(remote_path) : null;

                        if (remote_info == null || (remote_info.IsRegularFile && behavior == FolderUploadBehavior.AddOverwrite))
                            UploadFile(path, remote_path);
                    }
                }
                catch (Exception ex)
                {
                    problems.Add(ex);
                }
            }

            if (problems.Any())
                throw new AggregateException(problems);
        }

        public void DownloadFile(string remoteSource, string localDestionation)
        {
            remoteSource = remoteSource.FixPathForSFTP();
            using (var writer = File.Create(localDestionation))
                client.DownloadFile(remoteSource, writer);
        }

        /// <summary>
        /// Stáhne složku ze serveru.
        /// </summary>
        /// <param name="remoteSource"></param>
        /// <param name="localDestination"></param>
        /// <param name="behavior"></param>
        public void DownloadFolder(string remoteSource, string localDestination, FolderUploadBehavior behavior)
        {
            remoteSource = remoteSource.FixPathForSFTP();

            if (!client.Exists(remoteSource))
                throw new SftpPathNotFoundException("Složka na serveru není.");

            var files = client.ListDirectory(remoteSource);

            if (Directory.Exists(localDestination))
            {
                if (behavior == FolderUploadBehavior.ReplaceWhole)
                    Directory.Delete(localDestination, true);
                behavior = FolderUploadBehavior.AddOverwrite;
            }
            else
                Directory.CreateDirectory(localDestination);

            List<Exception> problems = new List<Exception>();
            foreach(var file in files)
            {
                try
                {
                    string localPath = Path.Combine(remoteSource, Path.GetFileName(file.FullName));

                    if (file.IsDirectory)
                    {
                        DownloadFolder(file.FullName, localPath, behavior);
                    }
                    else if (file.IsRegularFile)
                    {
                        if (File.Exists(localPath) && behavior == FolderUploadBehavior.AddKeep)
                            continue;

                        DownloadFile(file.FullName, localPath);
                    }
                }
                catch (Exception ex)
                {
                    problems.Add(ex);
                }
            }

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
            client.Delete(remoteFile.FixPathForSFTP());
        }

        /// <summary>
        /// Vytvoří složku
        /// </summary>
        /// <param name="remoteDestination"></param>
        public void CreateDirectory(string remoteDestination)
        {
            string[] paths = remoteDestination.PathProgression();
            for (int i = 1; i < paths.Length; i++)
            {
                //pokud složka o úroveň výš neobsahuje složku, kterou chceme, aby obsahovala, musíme jí vytvořit
                if (!client.ListDirectory(paths[i - 1])
                    .Any(f => f.IsDirectory && f.Name == Path.GetFileName(paths[i])))
                {
                    client.CreateDirectory(paths[i].FixPathForSFTP());
                }
            }
        }

        public SftpFile GetFile(string path)
        {
            if (!client.Exists(path))
                return null;

            var file = client.Get(path);
            return file.IsRegularFile ? file : null;
        }

        public SftpFile GetDirectory(string path)
        {
            if (!client.Exists(path))
                return null;

            var file = client.Get(path);
            return file.IsDirectory ? file : null;
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
}
