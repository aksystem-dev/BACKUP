using SmartModulBackupClasses;
using SmartModulBackupClasses.Managers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace smart_modul_BACKUP_service
{
    /// <summary>
    /// Odstraňuje staré zálohy
    /// </summary>
    public class BackupCleaner
    {
        /// <summary>
        /// Zdali automaticky odstraňovat informace o zálohách, které již nejsou dostupné (ani lokálně, ani vzdáleně)
        /// </summary>
        public bool RemoveDeadInfos { get; set; } = true;

        private Task _task = null;

        /// <summary>
        /// Projde všechna pravidla a pro každé provede vyčištění starých záloh.
        /// </summary>
        /// <returns></returns>
        public Task CleanupAllRulesAsync()
        {
            if (_task != null)
                return _task;

            _task = Task.Run(doTheCleanup).ContinueWith(task => _task = null);
            return _task;
        }

        /// <summary>
        /// CleanupAllRulesAsync().Wait()
        /// </summary>
        public void CleanupAllRules()
        {
            CleanupAllRulesAsync().Wait();
        }

        /// <summary>
        /// Projde všechny pravidla, pro každé pravidlo projde všechny zálohy podle něj vytvořené,
        /// a vymaže je podle toho, jak je pravidlo nastaveno; může vymazat lokální zálohy i zálohy na sftp
        /// </summary>
        private void doTheCleanup()
        {
            var rule_man = Manager.Get<BackupRuleLoader>();
            var bk_man = Manager.Get<BackupInfoManager>();
            var sftp = Manager.Get<SftpUploader>(true);

            foreach (var rule in rule_man.Rules)
            {
                try
                {
                    cleanuprule(rule, sftp, bk_man);
                }
                catch (Exception ex)
                {
                    SmbLog.Error($"Nepodařilo se pročistit zálohy pravidla {rule.Name}", ex, LogCategory.BackupCleaner);
                }
            }
        }

        /// <summary>
        /// Projde všechny zálohy vytvořené na tomto PC dle daného pravidla a podle konfigurace pravidla
        /// vymaže staré zálohy.
        /// </summary>
        /// <param name="rule">Pravidlo ke kontrole</param>
        /// <param name="bk_man">BackupInfoManager k použití; nechá-li se null, vezmeme Manager.Get<BackupInfoManager>()</BackupInfoManager></param>
        private void cleanuprule(BackupRule rule, SftpUploader sftp = null, BackupInfoManager bk_man = null)
        {
            SmbLog.Info($"cleanuprule pravidla {rule.Name}", category: LogCategory.BackupCleaner);

            bk_man = bk_man ?? Manager.Get<BackupInfoManager>();

            //zde porychtujem sftp připojení, které bude potřeba
            sftp = sftp ?? Manager.Get<SftpUploader>(true);

            //seznam záloh z tohoto pravidla seřazený sestupně podle data (nejnovější zálohy - první)
            var rel_bks = bk_man.LocalBackups
                .Where(b => b.RefRule == rule.LocalID)
                .OrderByDescending(b => b.EndDateTime);

            //projít všechny zálohy, pro každou zjistit, zdali jí máme odstranit, a povolat metodu evaluateBackup
            int i = 0;
            foreach(var bk in rel_bks)
            {
                bool deleteLocal = rule.LocalBackups.ShouldDelete(bk, i);
                bool deleteRemote = rule.RemoteBackups.ShouldDelete(bk, i);

                SmbLog.Debug($"Záloha {bk.GenInfoFileName()}: deleteLocal = {deleteLocal}, deleteRemote = {deleteRemote}", category: LogCategory.BackupCleaner);

                evaluateBackup(bk, deleteLocal, deleteRemote, bk_man, sftp);

                i++;
            }
        }

        /// <summary>
        /// Porychtuje jednu konkrétní zálohu.
        /// </summary>
        /// <param name="bk">Objekt s info o záloze</param>
        /// <param name="deleteLocal">Jestli odstranit lokální soubor zálohy</param>
        /// <param name="deleteRemote">Jestli odstranit remote soubor zálohy</param>
        /// <param name="bk_man">Správce info o záloháx</param>
        /// <param name="sftp">Připojení k sftp serveru (uvnitř metody se nevolá connect ani disconnect, o to se postarej ty)</param>
        private void evaluateBackup(Backup bk, bool deleteLocal, bool deleteRemote, BackupInfoManager bk_man, SftpUploader sftp)
        {
            SmbLog.Debug("Zavoláno evaluateBackup", category: LogCategory.BackupCleaner);

            //zde kváknem jestli jsme změnili AvailableLocally nebo AvailableRemotely, ať víme, jestli updatovat záznam
            bool availabilityChanged = false;

            if (bk.AvailableLocally && bk.MadeOnThisComputer)
            {
                //pokud záloha tvrdí, že existuje na tomto PC, ale přitom nikoliv, vysvětlíme jí, jak se věci mají
                if (!bk.DoesLocalFileExist())
                {
                    SmbLog.Debug("Záloha tvrdí, že je dostupná lokálně, ale není, vysvětluju jí to", category: LogCategory.BackupCleaner);

                    bk.AvailableLocally = false;
                    availabilityChanged = true;
                }
                //pokud záloha existuje lokálně a máme jí smazat, smažem jí
                else if(deleteLocal)
                {
                    try
                    {
                        if (bk.IsZip)
                        {
                            File.SetAttributes(bk.LocalPath, FileAttributes.Normal);
                            File.Delete(bk.LocalPath);
                        }
                        else
                            FileUtils.DeleteFolder(bk.LocalPath, exceptionBehavior: ItemExceptionBehavior.ThrowException);

                        bk.AvailableLocally = false;
                        availabilityChanged = true;
                    }
                    catch (Exception ex)
                    {
                        SmbLog.Error("Problém při odstraňování lokální zálohy.", ex, LogCategory.BackupCleaner);
                    }
                }
            }

            if (bk.AvailableRemotely && sftp != null && bk.AvailableOnCurrentSftpServer)
            {
                try
                {
                    //pokud záloha tvrdí, že je dostupná přes aktuální sftp, ale není, vysvětlíme jí, jak se věci mají
                    if (!bk.DoesRemoteFileExist(sftp.client))
                    {
                        SmbLog.Info("Záloha tvrdí, že je dostupná vzdáleně, ale není, vysvětluji jí to", category: LogCategory.BackupCleaner);

                        bk.AvailableRemotely = false;
                        availabilityChanged = true;
                    }
                    //jinak pokud existuje na sftp, ale máme jí odstranit, dáme se do toho
                    else if (deleteRemote)
                    {
                        try
                        {
                            if (bk.IsZip)
                            {
                                var file = sftp.GetFile(bk.RemotePath);
                                file.Delete();
                            }
                            else
                            {
                                var folder = sftp.GetDirectory(bk.RemotePath);
                                sftp.DeleteDirectory(folder.FullName);
                            }

                            bk.AvailableRemotely = false;
                            availabilityChanged = true;
                        }
                        catch (Exception ex)
                        {
                            SmbLog.Error("Problém při odstraňování vzdálené zálohy.", ex, LogCategory.BackupCleaner);
                        }
                    }
                }
                catch { }
            }

            //pokud záloha není dostupná ani lokálně ani vzdáleně, můžeme o ní odstranit informaci, páč je zbytečná
            if (RemoveDeadInfos && !bk.AvailableRemotely && !bk.AvailableLocally)
            {
                SmbLog.Info("Záloha není dostupná ani lokálně ani vzdáleně, mažu o ní info...", category: LogCategory.BackupCleaner);
                bk_man.DeleteAsync(bk, sftp).Wait();
            }
            //jinak pokud jsme změnili v tomto AvailableLocally nebo AvailableRemotely, měli bychom změněné info uložit
            else if (availabilityChanged)
                bk_man.UpdateAsync(bk, sftp).Wait();
        }
    }
}
