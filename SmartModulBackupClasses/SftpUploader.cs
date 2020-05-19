using Renci.SshNet;
using SmartModulBackupClasses;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartModulBackupClasses
{
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

        public void Upload(string localSource, string remoteDestination)
        {
            //Logger.Log($"SFTP Upload: {localSource} -> {remoteDestination}");

            CreateDirectory(remoteDestination.PathMoveUp());

            //Logger.Log($"Kopíruji lokální soubor {localSource} přes SFTP na {remoteDestination}");

            using (var stream = File.OpenRead(localSource))
                client.UploadFile(stream, remoteDestination.FixPathForSFTP());
        }

        public void Delete(string remoteFile)
        {
            //Logger.Log($"client.delete({remoteFile.FixPathForSFTP()})");
            client.Delete(remoteFile.FixPathForSFTP());
        }

        public void CreateDirectory(string remoteDestination)
        {
            string[] paths = remoteDestination.PathProgression();
            for (int i = 1; i < paths.Length; i++)
            {
                //Logger.Log($"client.ListDirectory({paths[i - 1]})");
                //pokud složka o úroveň výš neobsahuje složku, kterou chceme, aby obsahovala, musíme jí vytvořit
                if (!client.ListDirectory(paths[i - 1])
                    .Any(f => f.IsDirectory && f.Name == Path.GetFileName(paths[i])))
                {
                    //Logger.Log($"client.CreateDirectory({paths[i]})");
                    client.CreateDirectory(paths[i].FixPathForSFTP());
                }
            }
        }

        public void Dispose()
        {
            client.Dispose();
        }
    }
}
