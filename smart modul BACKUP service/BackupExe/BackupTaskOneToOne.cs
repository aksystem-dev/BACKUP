using SmartModulBackupClasses;
using SmartModulBackupClasses.Managers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            Progress?.Update(BackupState.Initializing, 0);

            cfg = Manager.Get<ConfigManager>().Config;

            //DateTime lastBkDateTime =
            //    Manager.Get<BackupInfoManager>().LocalBackups.FirstOrDefault(bk => bk.RefRule == Rule.LocalID)?.StartDateTime ?? new DateTime();

            //Vytvořit objekt s informacemi o záloze
            B_Obj = Backup.New(Rule, bk =>
            {
                bk.OneToOneStatus = new OneToOneBackupStatus();
                bk.LocalPath = Path.GetFullPath(Path.Combine(cfg.LocalBackupDirectory, Rule.Name, "OneToOne"));
                bk.RemotePath = Rule.RemoteBackups.enabled ? Path.Combine(SMB_Utils.GetRemoteBackupPath(), Rule.Name, "OneToOne") : null;
                bk.AvailableLocally = Rule.LocalBackups.enabled;
                bk.AvailableRemotely = Rule.RemoteBackups.enabled;
            });
            await Manager.Get<BackupInfoManager>().AddQuietlyAsync(B_Obj);

            if (IsCancelled) goto finish;
            Progress?.Update(BackupState.RunningProcesses, 0);
            if (!runProcesses()) goto finish;

            if (IsCancelled) goto finish;
            Progress?.Update(BackupState.ConnectingSftp, 0);
            connectSftp();

            if (IsCancelled) goto finish;
            Progress?.Update(BackupState.CreatingVss, 0);
            vss();

            float src_total = (float)Rule.Sources.Directories.Count();
            int curr = 0;

            //projít zdroje
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

                //lokální záloha zdroje
                if (Rule.LocalBackups.enabled)
                {
                    Progress?.Update(BackupState.OneToOneBackups, curr / src_total, $"LOKÁLNÍ ZÁLOHA ZDROJE {src.path}");
                    if (!backupOneToOneLocalDir(src.path))
                    {
                        success = false;
                        saved.ErrorDetail += "Vyskytly se problémy při zálohování do lokálního úložiště.\n";
                    }
                }

                //vzdálená záloha zdroje
                if (Rule.RemoteBackups.enabled)
                {
                    Progress?.Update(BackupState.OneToOneBackups, curr / src_total, $"VZDÁLENÁ ZÁLOHA ZDROJE {src.path}");
                    if (!backupOneToOneSftp(src.path, curr / src_total, (curr + 1) / src_total))
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

                curr++;
            }

        finish:

            if (IsCancelled)
                error("Záloha byla zrušena.");

            Progress?.Update(IsCancelled ? BackupState.Cancelling : BackupState.Finishing, 0);
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

        private bool backupOneToOneSftp(string src, float pg_from, float pg_to)
        {
            try
            {
                string root = Path.GetPathRoot(src);
                string dest = Path.Combine(B_Obj.RemotePath, Path.GetFileName(src));
                logInfo($"Záloha 1:1 sftp: {src} >> {dest}");

                //cesta, z které nahráváme (podle toho, jestli shadow copy nebo ne)
                string path = shadowCopies.ContainsKey(root) ? shadowCopies[root].GetShadowPath(src) : src; 

                float tot_size = (float)FileUtils.GetDirSize(path);

                //průběžně budeme posílat GUI info o tom, kolik bytů již bylo staženo.
                //nechceme to ale posílat moc často, takže to ohlídáme pomocí Stopwatch.
                Stopwatch progress_interval_limiter = new Stopwatch();

                sftp.UploadDirectoryDiff(path, dest, Rule.OneToOneDelete, ul =>
                {
                    if (progress_interval_limiter.ElapsedMilliseconds > Const.UPDATE_GUI_SFTP_UPLOAD_MIN_MS_INTERVAL)
                    {
                        Progress?.Update(BackupState.OneToOneBackups, SMB_Utils.Lerp(pg_from, pg_to, ul / tot_size),
                            $"VZDÁLENÁ ZÁLOHA ZDROJE {path}");
                        progress_interval_limiter.Restart();
                    }
                });
                progress_interval_limiter.Stop();
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
