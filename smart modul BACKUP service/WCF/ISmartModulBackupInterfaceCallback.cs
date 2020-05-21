using SmartModulBackupClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace smart_modul_BACKUP_service.WCF
{
    public interface ISmartModulBackupInterfaceCallback
    {
        [OperationContract]
        void ShowError(string error);

        [OperationContract]
        void TestConnection();

        [OperationContract]
        void ShowMsg(string msg);

        /// <summary>
        /// Když se služba ukončuje
        /// </summary>
        [OperationContract]
        void Goodbye();

        [OperationContract]
        void StartRestore(RestoreInProgress progress);
        [OperationContract]
        void StartBackup(BackupInProgress progress);
        [OperationContract]
        void UpdateRestore(RestoreInProgress progress);
        [OperationContract]
        void UpdateBackup(BackupInProgress progress);
        [OperationContract]
        void CompleteRestore(RestoreInProgress progress, RestoreResponse response);
        [OperationContract]
        void CompleteBackup(BackupInProgress progress, int BackupID);
    }
}
