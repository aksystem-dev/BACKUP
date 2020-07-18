using SmartModulBackupClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smart_modul_BACKUP_service
{
    /// <summary>
    /// Udržuje seznam všech aktivních objektů BackupInProgress a RestoreInProgress.
    /// </summary>
    public class ProgressManager
    {
        private List<BackupInProgress> _backups = new List<BackupInProgress>();
        private List<RestoreInProgress> _restores = new List<RestoreInProgress>();

        public BackupInProgress[] Backups => _backups.ToArray();
        public RestoreInProgress[] Restores => _restores.ToArray();

        /// <summary>
        /// Přidá BackupInProgress
        /// </summary>
        /// <returns></returns>
        public BackupInProgress NewBackup()
        {
            int id = _backups.Any() ? _backups.Max(f => f.ProgressId) + 1 : 0;
            var backup = new BackupInProgress()
            {
                ProgressId = id
            };

            lock (_backups)
                _backups.Add(backup);

            return backup;
        }

        /// <summary>
        /// Odstraní BackupInProgress
        /// </summary>
        /// <param name="backup"></param>
        /// <returns></returns>
        public bool RemoveBackup(BackupInProgress backup)
        {
            lock (_backups)
                return _backups.Remove(backup);
        }

        /// <summary>
        /// Přidá RestoreInProgress
        /// </summary>
        /// <returns></returns>
        public RestoreInProgress NewRestore()
        {
            int id = _backups.Any() ? _backups.Max(f => f.ProgressId) + 1 : 0;
            var restore = new RestoreInProgress()
            {
                ProgressId = id
            };

            lock (_restores)
                _restores.Add(restore);

            return restore; 
        }

        /// <summary>
        /// Odstraní RestoreInProgress
        /// </summary>
        /// <param name="restore"></param>
        /// <returns></returns>
        public bool RemoveRestore(RestoreInProgress restore)
        {
            lock (_restores)
                return _restores.Remove(restore);
        }
    }
}
