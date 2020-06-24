using SmartModulBackupClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace smart_modul_BACKUP_service.WCF
{
    // POZNÁMKA: Pomocí příkazu Přejmenovat v nabídce Refaktorovat můžete změnit název rozhraní IService1 společně v kódu a konfiguračním souboru.
    [ServiceContract(SessionMode = SessionMode.Required, CallbackContract = typeof(ISmartModulBackupInterfaceCallback))]
    public interface ISmartModulBackupInterface
    {
        [OperationContract]
        void Reload();

        [OperationContract]
        int[] GetRunningBackups();

        [OperationContract]
        BackupInProgress DoSingleBackup(string ruleXml);
        //BackupInProgress DoSingleBackup(int rule);

        [OperationContract]
        RestoreInProgress Restore(Restore restoreInfo);

        [OperationContract]
        BackupInProgress[] GetBackupsInProgress();

        [OperationContract]
        RestoreInProgress[] GetRestoresInProgress();

        [OperationContract]
        void Connect();

        [OperationContract]
        void Disconnect();

        [OperationContract]
        void ImStillHere();

        [OperationContract]
        void UpdateApi();

        [OperationContract]
        void ReloadConfig();
        
        [OperationContract]
        void SetRule(string ruleXml);

        [OperationContract]
        void CleanupBackups();
    }
}
