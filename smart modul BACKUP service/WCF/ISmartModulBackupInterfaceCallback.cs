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
        void BackupStarted(string ruleName);

        [OperationContract]
        void BackupEnded(string ruleName, bool success);

        [OperationContract]
        void ShowError(string error);

        [OperationContract]
        void TestConnection();

        [OperationContract]
        void ShowMsg(string msg);

        [OperationContract]
        void RestoreComplete(RestoreResponse response);

        /// <summary>
        /// Když se služba ukončuje
        /// </summary>
        [OperationContract]
        void Goodbye();
    }
}
