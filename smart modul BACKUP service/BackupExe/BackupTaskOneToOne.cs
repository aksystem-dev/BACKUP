using SmartModulBackupClasses;
using SmartModulBackupClasses.Managers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smart_modul_BACKUP_service.BackupExe
{
    public partial class BackupTask
    {
        /// <summary>
        /// <para>Záloha OneToOne - do cílových umístění se zkopírují pouze soubory, které se od poslední 
        /// zálohy změnily</para>
        /// Narozdíl od backupFull se v tomto případě v cílových umístění udržuje pouze jedna verze
        /// zálohy, a ta je ve složce OneToOne
        /// </summary>
        /// <returns></returns>
        private async Task backupOneToOne()
        {
            Progress?.Update("INICIALIZACE", 0);

            cfg = Manager.Get<ConfigManager>().Config;

            //DateTime lastBkDateTime =
            //    Manager.Get<BackupInfoManager>().LocalBackups.FirstOrDefault(bk => bk.RefRule == Rule.LocalID)?.StartDateTime ?? new DateTime();

            //Vytvořit objekt s informacemi o záloze
            B_Obj = new Backup()
            {
                RefRule = Rule.LocalID,
                RefRuleName = Rule.Name,
                BackupType = Rule.RuleType,
                Errors = new List<BackupError>(),
                Sources = new List<SavedSource>(),
                Success = true,
                StartDateTime = DateTime.Now,
                ComputerId = SMB_Utils.GetComputerId(),
                Saved = false,
                IsZip = Rule.Zip,
                OneToOneStatus = new OneToOneBackupStatus(),
                LocalPath = Path.GetFullPath(Path.Combine(cfg.LocalBackupDirectory, Rule.Name, "OneToOne")),
                RemotePath = Rule.RemoteBackups.enabled ? Path.Combine(SMB_Utils.GetRemoteBackupPath(), Rule.Name, "OneToOne") : null,
                AvailableLocally = Rule.LocalBackups.enabled,
                AvailableRemotely = Rule.RemoteBackups.enabled,
                SftpHash = Rule.RemoteBackups.enabled ? SMB_Utils.GetSftpHash() : null,
                PlanId = SMB_Utils.GetCurrentPlanId()
            };
            await Manager.Get<BackupInfoManager>().AddQuietlyAsync(B_Obj);

            if (IsCancelled) goto finish;
            Progress?.Update("SPOUŠTÍM PROCESY", 0.05f);
            if (!runProcesses()) goto finish;

            if (IsCancelled) goto finish;
            Progress?.Update("PŘIPOJUJI SFTP", 0.15f);
            connectSftp();

            if (IsCancelled) goto finish;
            Progress?.Update("VYTVÁŘÍM SHADOW COPY", 0.25f);
            vss();

            Progress?.Update("ZÁLOHUJI ZDROJE", 0.35f);
            foreach (var src in Rule.Sources.Directories.Where(f => f.enabled))
            {
                logInfo($"Jdu na zdroj {src.id}");

                if (IsCancelled) goto finish;

                var saved = new SavedSource()
                {
                    filename = Path.GetFileName(src.path),
                    sourcepath = src.path,
                    Error = "",
                    ErrorDetail = "\n",
                    type = BackupSourceType.Directory,
                };

                bool success = true;

                if (Rule.LocalBackups.enabled)
                {
                    if(!backupOneToOneLocalDir(src.path))
                    {
                        success = false;
                        saved.ErrorDetail += "Vyskytly se problémy při zálohování do lokálního úložiště.\n";
                    }

                }

                if (Rule.RemoteBackups.enabled)
                {
                    if(!backupOneToOneSftp(src.path))
                    {
                        success = false;
                        saved.Error += "Vyskytly se problémy při zálohování do vzdáleného úložiště.\n";
                    }
                }

                logInfo($"Zdroj {src.id} porychtován");

                saved.ErrorDetail = saved.ErrorDetail.Substring(0, saved.ErrorDetail.Length - 1);
                if (!success)
                {
                    saved.Error = "Došlo k problémům.";
                    saved.Success = SuccessLevel.SomeErrors;
                }
                else
                    saved.Success = SuccessLevel.EverythingWorked;
                
                //uložit info o tomto zdroji
                B_Obj.Sources.Add(saved);

                //přidat velikost tohoto zdroje do celkové velikosti zálohy
                try
                {
                    B_Obj.Size += FileUtils.GetDirSize(src.path);
                }
                catch (Exception ex)
                {
                    error("problém při měření velikosti složky", ex);
                }
            }

        finish:

            if (IsCancelled)
                error("Záloha byla zrušena.");

            Progress?.Update(IsCancelled ? "RUŠÍM ZÁLOHU" : "UKONČUJI ZÁLOHU", 0.8f);
            await finishBackup();
        }

        private bool backupOneToOneLocalDir(string src)
        {
            string dest = Path.Combine(B_Obj.LocalPath, Path.GetFileName(src));
            logInfo($"Záloha 1:1 lokální: {src} >> {dest}");

            List<string> failed_paths = new List<string>();

            var root = Path.GetPathRoot(src);
            bool copied_all = FileUtils.CopyFolderContentsDiff(
                //pokud máme pro tento volume Shadow Copy, budeme zipovat Shadow Copy; jinak normálně ten soubor
                shadowCopies.ContainsKey(root) ? shadowCopies[root].GetShadowPath(src) : src,
                dest, delete: Rule.OneToOneDelete, errorPaths: failed_paths);

            if (!copied_all)
                error("Nepodařilo se zálohovat některé soubory lokálně");

            return copied_all;
        }

        private bool backupOneToOneSftp(string src)
        {
            try
            {
                string root = Path.GetPathRoot(src);
                string dest = Path.Combine(B_Obj.RemotePath, Path.GetFileName(src));
                logInfo($"Záloha 1:1 sftp: {src} >> {dest}");
                sftp.UploadDirectoryDiff(shadowCopies.ContainsKey(root) ? shadowCopies[root].GetShadowPath(src) : src,
                    dest, Rule.OneToOneDelete);
                return true;
            }
            catch (Exception ex)
            {
                error("Problém při nahrávání 1:1 zálohy na server", ex);
                return false;
            }
        }
    }
}
