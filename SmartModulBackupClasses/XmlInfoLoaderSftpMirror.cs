using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartModulBackupClasses
{
    /// <summary>
    /// Načítá informace z lokálních souborů a zároveň ze souboru přes sftp.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class XmlInfoLoaderSftpMirror<T> : XmlInfoLoader<T> where T : IHaveID
    {
        public string RemotePath { get; private set; }
        public BackupLocation Priority { get; private set; }
        private SftpUploaderFactory _sftp;
        
        public XmlInfoLoaderSftpMirror(string localFile, SftpUploaderFactory sftp, 
            string remoteFile = null, BackupLocation priority = BackupLocation.SFTP) : base(localFile)
        {
            RemotePath = remoteFile ?? localFile;
            Priority = priority;
            _sftp = sftp;
        }

        public override void LoadInfos()
        {
            ClearInfos();

            SMB_Log.Log("XmlInfoLoaderSftpMirror.LoadInfos()");

            var sftp = _sftp?.GetInstance();
            if (sftp?.TryConnect(1000) == false)
                sftp = null;

            if (sftp == null)
            {
                base.LoadInfos();
                return;
            }

            try
            {
                if (Priority == BackupLocation.SFTP)
                {
                    _loadFromSftp(sftp);
                    _loadInfos(append: true);
                }
                else
                {
                    _loadInfos(append: true);
                    _loadFromSftp(sftp);
                }
            }
            catch (Exception e)
            {
                SMB_Log.Log($"XmlInfoLoaderSftpMirror.LoadInfos() error: {e.GetType().Name}\n\n{e.Message}");
                throw e;
            }
            finally
            {
                sftp.Disconnect();
                sftp.client.Dispose();
            }
        }

        public override void SaveInfos()
        {
            base.SaveInfos();

            SMB_Log.Log("XmlInfoLoaderSftpMirror.SaveInfos()");

            var sftp = _sftp?.GetInstance();
            if (sftp?.TryConnect(1000) == false)
                sftp = null;

            if (sftp == null)
                return;

            try
            {
                sftp.Upload(LocalPath, RemotePath);
            }
            catch (Exception e)
            {
                SMB_Log.Log($"XmlInfoLoaderSftpMirror.SaveInfos() error: {e.GetType().Name}\n\n{e.Message}");
                throw e;
            }
            finally
            {
                sftp.Disconnect();
                sftp.client.Dispose();
            }
        }

        private void _loadFromSftp(SftpUploader sftp)
        {
            if (sftp.client.Exists(RemotePath.FixPathForSFTP()))
            {
                string text = sftp.client.ReadAllText(RemotePath.FixPathForSFTP());
                SMB_Log.Log($"XmlInfoLoaderSftpMirror._loadFromSftp(): staženo \"{RemotePath}\":\n\n{text}");
                _loadInfos(append: true, xml: text);
            }
            else
            {
                SMB_Log.Log($"XmlInfoLoaderSftpMirror._loadFromSftp(): file {RemotePath} neexistuje");
            }
        }
    }
}
