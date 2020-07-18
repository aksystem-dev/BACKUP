using SmartModulBackupClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace smart_modul_BACKUP_service.WCF
{
    /// <summary>
    /// Umožňuje komunikaci s několika GUI najednou.
    /// </summary>
    class CallbackToMany : ISmartModulBackupInterfaceCallback
    {
        public List<ISmartModulBackupInterfaceCallback> callbacks { get; private set; }
            = new List<ISmartModulBackupInterfaceCallback>();

        private void ForEachCallback(Action<ISmartModulBackupInterfaceCallback> action)
        {
            foreach(var cb in callbacks.ToArray())
            {
                try
                {
                    action(cb);
                }
                catch (Exception e)
                {
                    SmbLog.Error("Problém při volání metody na callback objektu", e, LogCategory.ServiceHost);
                    callbacks.Remove(cb);
                }
            }
        }


        public void ShowError(string error) => ForEachCallback(f => f.ShowError(error));
        public void ShowMsg(string msg) => ForEachCallback(f => f.ShowMsg(msg));
        public void TestConnection() => ForEachCallback(f => f.TestConnection());
        public void Goodbye() => ForEachCallback(f => f.Goodbye());
        //public void SetProgress(int a, string b, float c) => ForEachCallback(f => f.SetProgress(a, b, c));
        //public void RemoveProgressBar(int bar_id) => ForEachCallback(f => f.RemoveProgressBar(bar_id));

        public bool AddCallback(ISmartModulBackupInterfaceCallback callback)
        {
            if (callbacks.Contains(callback))
                return false;

            callbacks.Add(callback);
            return true;
        }

        public bool RemoveCallback(ISmartModulBackupInterfaceCallback callback)
        {
            if (!callbacks.Contains(callback))
                return false;

            callbacks.Remove(callback);
            return true;
        }

        public void StartRestore(RestoreInProgress progress) => ForEachCallback(f => f.StartRestore(progress));
        public void StartBackup(BackupInProgress progress) => ForEachCallback(f => f.StartBackup(progress));
        public void UpdateRestore(RestoreInProgress progress) => ForEachCallback(f => f.UpdateRestore(progress));
        public void UpdateBackup(BackupInProgress progress) => ForEachCallback(f => f.UpdateBackup(progress));
        public void CompleteRestore(RestoreInProgress progress, RestoreResponse response) 
            => ForEachCallback(f => f.CompleteRestore(progress, response));
        public void CompleteBackup(BackupInProgress progress, int BackupID)
            => ForEachCallback(f => f.CompleteBackup(progress, BackupID));
    }
}
