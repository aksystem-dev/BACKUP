using SmartModulBackupClasses;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smart_modul_BACKUP
{
    /// <summary>
    /// Obsahuje informace o probíhajících zálohách a obnovách. Má seznam BackupInProgress a RestoreInProgress.
    /// </summary>
    public class InProgress : INotifyPropertyChanged
    {
        private ServiceState service => Manager.Get<ServiceState>();
        private List<BackupInProgress> _backups = new List<BackupInProgress>();
        private List<RestoreInProgress> _restores = new List<RestoreInProgress>();

        public BackupInProgress[] Backups => _backups.ToArray();
        public RestoreInProgress[] Restores => _restores.ToArray();

        public event PropertyChangedEventHandler PropertyChanged;
        private void _b_ch() =>
            App.dispatch(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Backups))));
        private void _r_ch() =>
            App.dispatch(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Restores))));

        /// <summary>
        /// Pokud záloha se stejným id již je na seznamu, updatuje ji; jinak ji tam přidá
        /// </summary>
        /// <param name="backup"></param>
        /// <returns></returns>
        public BackupInProgress SetBackup(BackupInProgress backup)
        {
            var found = _backups.FirstOrDefault(f => f.ProgressId == backup.ProgressId);

            if (ReferenceEquals(backup, found))
                return backup;

            if (found != null)
            {
                Reflector.ShallowMirror(backup, found);
                
                if (backup.Errors.Count > found.Errors.Count)
                {
                    for (int i = found.Errors.Count - 1; i < backup.Errors.Count; i++)
                        found.Errors.Add(backup.Errors[i]);
                }

                App.dispatch(() => found.Refresh());
                return found;
            }
            else
            {
                lock (_backups)
                    _backups.Add(backup);

                _b_ch();
                return backup;
            }
        }

        /// <summary>
        /// Pokud obnova se stejným id již je na seznamu, updatuje ji; jinak ji tam přidá
        /// </summary>
        /// <param name="restore"></param>
        /// <returns></returns>
        public RestoreInProgress SetRestore(RestoreInProgress restore)
        {
            var found = _restores.FirstOrDefault(f => f.ProgressId == restore.ProgressId);

            if (ReferenceEquals(restore, found))
                return restore;

            if (found != null)
            {
                Reflector.ShallowMirror(restore, found);

                if (restore.Errors.Count > found.Errors.Count)
                {
                    for (int i = found.Errors.Count - 1; i < restore.Errors.Count; i++)
                        found.Errors.Add(restore.Errors[i]);
                }

                App.dispatch(() => found.Refresh());

                return found;
            }
            else
            {
                lock (_restores)
                    _restores.Add(restore);

                _r_ch();
                return restore;
            }
        }

        /// <summary>
        /// Nahraje info o probíhajících zálohách ze služby
        /// </summary>
        public void FetchBackups()
        {
            //zde zůstanou pouze zálohy, které jsou nyní na seznamu, ale služba je nevrátila
            List<BackupInProgress> forgotten = _backups.ToList();

            //pro každou zálohu, kterou služba vrátí, jí updatujeme, a odstraníme ze seznamu forgotten
            foreach (var bak in service.GetBackupsInProgress())
                forgotten.Remove(SetBackup(bak));

            //odstranit všechny zapomenuté zálohy
            foreach(var bak in forgotten)
            {
                _backups.Remove(bak);
                bak.Complete();
            }

            //updatovat zobrazení
            _b_ch();
        }

        public void FetchRestores()
        {
            //zde zůstanou pouze zálohy, které jsou nyní na seznamu, ale služba je nevrátila
            List<RestoreInProgress> forgotten = _restores.ToList();

            //pro každou zálohu, kterou služba vrátí, jí updatujeme, a odstraníme ze seznamu forgotten
            foreach (var res in service.GetRestoresInProgresses())
                forgotten.Remove(SetRestore(res));

            //odstranit všechny zapomenuté zálohy
            foreach (var res in forgotten)
            {
                _restores.Remove(res);
                res.Complete();
            }

            //updatovat zobrazení
            _r_ch();
        }

        public void FetchData()
        {
            FetchBackups();
            FetchRestores();
        }

        public BackupInProgress GetBackup(int id) => _backups.FirstOrDefault(b => b.ProgressId == id);
        public RestoreInProgress GetRestore(int id) => _restores.FirstOrDefault(r => r.ProgressId == id);

        public bool RemoveBackup(int id)
        {
            lock(_backups)
            {
                var bak = GetBackup(id);
                if (bak != null)
                {
                    _backups.Remove(bak);
                    _b_ch();
                    return true;
                }
            }

            return false;
        }

        public bool RemoveRestore(int id)
        {
            lock (_restores)
            {
                var res = GetRestore(id);
                if (res != null) 
                {
                    _restores.Remove(res);
                    _r_ch();
                    return true;
                }
            }

            return false;
        }
    }
}
