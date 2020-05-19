using SmartModulBackupClasses;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smart_modul_BACKUP_service
{
    class BackupCleaner
    {
        public SftpUploaderFactory sftpFactory;
        public XmlInfoLoader<Backup> SavedBackups;

        public void CleanUpRule(BackupRule rule, List<Backup> backups = null)
        {
            List<Backup> ruleBackups = backups ?? SavedBackups.GetInfos().Where(f => f.RefRule == rule.LocalID).ToList();

            var localBaks = ruleBackups.Where(f => f.AvailableLocally == true);
            foreach (var bak in localBaks)
            {
                CheckForBackup_Local(bak);
            }

            var remoteBaks = ruleBackups.Where(f => f.AvailableRemotely);
            foreach(var bak in remoteBaks)
            {
                CheckForBackup_SFTP(bak);
            }

            foreach(var bak in backups)
            {
                if (!bak.AvailableRemotely && !bak.AvailableLocally)
                    SavedBackups.RemoveInfo(bak);
            }

            SavedBackups.SaveInfos();
        }
        
        private void CheckForBackup(Backup backup, SftpUploader sftp = null)
        {
            if (backup.AvailableRemotely)
                CheckForBackup_SFTP(backup, sftp);

            if (backup.AvailableLocally)
                CheckForBackup_Local(backup);
        }

        private void CheckForBackup_SFTP(Backup backup, SftpUploader sftp = null)
        {
            sftp = sftp ?? sftpFactory.GetInstance();
            if (!sftp.IsConnected) sftp.Connect();

            if (!sftp.client.Exists(backup.RemotePath))
                backup.AvailableRemotely = false;
        }

        private void CheckForBackup_Local(Backup backup)
        {
            if (!File.Exists(backup.LocalPath))
                backup.AvailableRemotely = false;
        }
    }
}
