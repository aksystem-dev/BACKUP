using smart_modul_BACKUP_service.Managers;
using smart_modul_BACKUP_service.RestoreExe;
using SmartModulBackupClasses;
using SmartModulBackupClasses.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace smart_modul_BACKUP_service.WCF
{

    // POZNÁMKA: Pomocí příkazu Přejmenovat v nabídce Refaktorovat můžete změnit název třídy Service1 společně v kódu a konfiguračním souboru.
    /// <summary>
    /// WCF objekt pro komunikaci s uživatelským rozhraním (zde implementovány metody, které může GUI volat)
    /// </summary>
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
    public class SmartModulBackupInterface : ISmartModulBackupInterface
    {
        public SmartModulBackupService serviceRef;
        public bool connected => callbacks.callbacks.Any();

        private CallbackToMany callbacks = new CallbackToMany();
        public ISmartModulBackupInterfaceCallback Callback => callbacks as ISmartModulBackupInterfaceCallback;
        public InstanceContext context { get; private set; }

        private static void logInfo(string info)
            => SmbLog.Info(info, null, LogCategory.ServiceHost);

        private static void logError(string error, Exception ex = null)
            => SmbLog.Error(error, null, LogCategory.ServiceHost);

        public SmartModulBackupInterface(SmartModulBackupService service)
        {
            serviceRef = service;
        }

        /// <summary>
        /// Vrátí všechny pravidla, která jsou momentálně vyhodnocována (WIP)
        /// </summary>
        /// <returns></returns>
        public int[] GetRunningBackups()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Oznámí službě, že se gui připojili
        /// </summary>
        public void Connect()
        {
            logInfo("Připojeno uživatelské rozhraní");

            //uložit odkaz na callback objekt, aby byl přístupný i z jiných vláken
            if (callbacks.AddCallback(OperationContext.Current.GetCallbackChannel<ISmartModulBackupInterfaceCallback>()))
                logInfo("Callback objekt uložen");
        }

        /// <summary>
        /// Znovu načte konfigurační soubory, pravidla, restartuje časovač, znovu naplánuje pravidla. Metoda pro GUI
        /// </summary>
        public void Reload()
        {
            logInfo("Přijat příkaz přes WCF: Reload()");
            serviceRef.Load();
        }

        public void Disconnect()
        {
            if (connected)
            {
                logInfo("Rozhraní se odpojilo");

                callbacks.RemoveCallback(OperationContext.Current.GetCallbackChannel<ISmartModulBackupInterfaceCallback>());
            }
        }

        /// <summary>
        /// Aby nevypršelo receiveTimeout, služba pravidelně posílá Callback kanálem GUI zprávu, na níž GUI odpovídá
        /// zavoláním této metody
        /// </summary>
        public void ImStillHere()
        {
            logInfo("Test připojení klienta úspěšný: klient je pořád připojený.");
        }

        public BackupInProgress DoSingleBackup(string ruleXml)
        {
            //BackupRule rule = Manager.Get<BackupRuleLoader>().Get(ruleId);
            var rule = BackupRule.LoadFromXmlStr(ruleXml);

            if (rule == null)
                return null;

            return rule.GetBackupTaskRightNow().Execute();
        }

        public RestoreInProgress Restore(Restore restoreInfo)
        {
            //var rip = Utils.InProgress.NewRestore();
            //Task.Run(() =>
            //{
            //    Manager.Get<Restorer>().Restore(restoreInfo, rip);
            //    Utils.InProgress.RemoveRestore(rip);
            //});

            var task = new RestoreTask(restoreInfo);
            return task.Start();
        }

        /// <summary>
        /// Vrátí všechny probíhající zálohy.
        /// </summary>
        /// <returns></returns>
        public BackupInProgress[] GetBackupsInProgress()
        {
            return Utils.InProgress.Backups;
           
        }

        /// <summary>
        /// Vrátí všechny probíhající obnovy.
        /// </summary>
        /// <returns></returns>
        public RestoreInProgress[] GetRestoresInProgress()
        {
            return Utils.InProgress.Restores;
        }

        /// <summary>
        /// Zavolá updataApi na SmartModulBackupService.
        /// </summary>
        public void UpdateApi()
        {
            try
            {
                Manager.Get<ConfigManager>().Load();
                SmartModulBackupService.updateApi(Manager.Get<ConfigManager>());
            }
            catch { }
        }

        /// <summary>
        /// Znovu načte konfiguraci ze souboru.
        /// </summary>
        public void ReloadConfig()
        {
            try
            {
                Manager.Get<ConfigManager>().Load();
            }
            catch { }
        }

        /// <summary>
        /// Nastaví pravidlo podle daného xml.
        /// </summary>
        /// <param name="ruleXml"></param>
        public void SetRule(string ruleXml)
        {
            try
            {
                var rule = Manager.Get<BackupRuleLoader>().SetRule(BackupRule.LoadFromXmlStr(ruleXml));
                Manager.Get<BackupTimeline>().RescheduleRule(rule);
            }
            catch { }
        }

        /// <summary>
        /// Odstraní staré zálohy.
        /// </summary>
        public void CleanupBackups()
        {
            try
            {
                Manager.Get<BackupCleaner>().CleanupAllRulesAsync();
            }
            catch { }
        }
    }
}
