using SmartModulBackupClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smart_modul_BACKUP_service
{
    public class ProgressManager
    {
        private List<BackupInProgress> _backups = new List<BackupInProgress>();
        private List<RestoreInProgress> _restores = new List<RestoreInProgress>();

        public BackupInProgress[] Backups => _backups.ToArray();
        public RestoreInProgress[] Restores => _restores.ToArray();

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

        public bool RemoveBackup(BackupInProgress backup)
        {
            lock (_backups)
                return _backups.Remove(backup);
        }

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

        public bool RemoveRestore(RestoreInProgress restore)
        {
            lock (_restores)
                return _restores.Remove(restore);
        }
    }
}
