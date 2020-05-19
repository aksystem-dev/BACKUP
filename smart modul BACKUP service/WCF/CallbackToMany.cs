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
                    Logger.Error($"Došlo k chybě při komunikaci s GUI {e.GetType().Name}\n\n{e.Message}\n\nOdpojuji se...");
                    callbacks.Remove(cb);
                }
            }
        }

        public void BackupEnded(string ruleName, bool success) => ForEachCallback(f => f.BackupEnded(ruleName, success));
        public void BackupStarted(string ruleName) => ForEachCallback(f => f.BackupStarted(ruleName));
        public void ShowError(string error) => ForEachCallback(f => f.ShowError(error));
        public void ShowMsg(string msg) => ForEachCallback(f => f.ShowMsg(msg));
        public void TestConnection() => ForEachCallback(f => f.TestConnection());
        public void RestoreComplete(RestoreResponse response) => ForEachCallback(f => f.RestoreComplete(response));
        public void Goodbye() => ForEachCallback(f => f.Goodbye());

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


    }
}
