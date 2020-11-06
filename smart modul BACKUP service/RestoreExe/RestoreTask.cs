using Renci.SshNet;
using Renci.SshNet.Common;
using smart_modul_BACKUP_service.BackupExe;
using SmartModulBackupClasses;
using SmartModulBackupClasses.Managers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace smart_modul_BACKUP_service.RestoreExe
{
    /// <summary>
    /// Představuje jednu konkrétní obnovu. Obsahuje kód pro provedení obnovy, který lze spustit
    /// zavoláním Start().
    /// </summary>
    public class RestoreTask
    {
        public const string TEMP_DIR = "temp_dir_restore";
        private static List<RestoreTask> _runningRestoreList = new List<RestoreTask>();
        public RestoreTask[] RunningRestoreTasks => _runningRestoreList.ToArray();

        public Restore Info { get; }
        private Backup bkToRestore;
        private RestoreInProgress progress;

        public TaskState State { get; private set; } = TaskState.NotStartedYet;
        public Task<RestoreResponse> TheTask { get; private set; }

        public RestoreTask(Restore info)
        {
            Info = info;
        }

        public RestoreInProgress Start()
        {
            if (State != TaskState.NotStartedYet)
                throw new InvalidOperationException("Tento task již byl spuštěn, nelze ho spustit znovu");

            var bakman = Manager.Get<BackupInfoManager>();
            bakman.LoadAsync().Wait();
            bkToRestore = bakman.Backups.FirstOrDefault(b =>
            {
                //berem pouze ty, které jsou z daného PC
                if (b.ComputerId != (Info.pcId ?? SMB_Utils.GetComputerId()))
                    return false;

                return b.LocalID == Info.backupID;
            });

            if (bkToRestore == null)
                throw new NullReferenceException("Záloha nenalezena!");

            progress = Utils.InProgress.NewRestore();
            progress.AfterUpdateCalled += () => Utils.GUIS.UpdateRestore(progress);
            Utils.GUIS.StartRestore(progress);

            lock (_runningRestoreList)
                _runningRestoreList.Add(this);

            Func<RestoreResponse> restoreMethod = null;
            switch (bkToRestore.BackupType)
            {
                case BackupRuleType.FullBackups:
                case BackupRuleType.ProtectedFolder:
                    if (bkToRestore.IsZip)
                        restoreMethod = restoreZip;
                    else
                        restoreMethod = restoreFolder;
                    break;
                case BackupRuleType.OneToOne:
                    restoreMethod = restoreFolder;
                    break;
                default:
                    throw new NotImplementedException($"? {bkToRestore.BackupType}");
            }

            TheTask = Task.Run(restoreMethod).ContinueWith(result =>
            {
                State = result.Status == TaskStatus.Faulted ? TaskState.Failed : TaskState.Finished;
                lock (_runningRestoreList)
                    _runningRestoreList.Remove(this);
                Utils.GUIS.CompleteRestore(progress, response);
                Manager.Get<ProgressManager>().RemoveRestore(progress);
                return result.Result;
            });

            return progress;
        }

        private SftpUploader sftp;
        private SqlBackuper sql;
        private string temp_instance_dir;
        private RestoreResponse response;
        private string zip_path;
        private string src_dir;

        private void logInfo(string msg)
        {
            SmbLog.Info(msg, null, LogCategory.RestoreTask);
        }

        private void bigError(string msg, Exception ex)
        {
            SmbLog.Error(msg, ex, LogCategory.RestoreTask);
            response.errors.Add(new Error(msg));
            response.Success = SuccessLevel.TotalFailure;
        }

        private void smallError(string msg, Exception ex, string detail = "")
        {
            SmbLog.Error(msg, ex, LogCategory.RestoreTask);
            response.errors.Add(new Error(msg, detail));
        }

        private RestoreResponse restoreFolder()
        {
            progress?.Update(RestoreState.Starting, 0);
            logInfo("Obnova složky započata;");

            Thread.Sleep(1500);

            response = new RestoreResponse(Info) { Success = SuccessLevel.EverythingWorked };
            createTempDir();

            if (Info.location == BackupLocation.SFTP)
                if (!getSftp()) goto finish;

            if (Info.sources.Any(src => src.type == BackupSourceType.Database))
                getSql();

            float src_count = Info.sources.Length;
            int i = 0;
            foreach (var src in Info.sources)
            {
                progress?.Update(RestoreState.RestoringSources, i / src_count, src.sourcepath);
                restoreLocalSourceFromPermanentLocation(src);
                i++;
            }

        finish:
            finishRestore();
            return response;
        }

        private RestoreResponse restoreZip()
        {
            progress?.Update(RestoreState.Starting, 0);
            logInfo("Obnova zipu započata.");

            Thread.Sleep(1500);

            response = new RestoreResponse(Info) { Success = SuccessLevel.EverythingWorked };
            createTempDir();

            if (Info.location == BackupLocation.SFTP)
                if (!getSftp()) goto finish;

            if (Info.sources.Any(src => src.type == BackupSourceType.Database))
                getSql();

            if (!getZip()) goto finish;

            if (!unZip()) goto finish;

            progress?.Update(RestoreState.RestoringSources, 0);

            float src_count = Info.sources.Length;
            int i = 0;
            foreach (var src in Info.sources)
            {
                progress?.Update(RestoreState.RestoringSources, i / src_count, src.sourcepath);
                restoreLocalSourceFromTempLocation(src);
                i++;
            }

            finish:
            finishRestore();
            return response;
        }

        /// <summary>
        /// Vytvoří složku pro tuto konkrétní obnovu a nastaví temp_instance_dir.
        /// </summary>
        private void createTempDir()
        {
            Directory.CreateDirectory(TEMP_DIR);
            temp_instance_dir = Path.Combine(TEMP_DIR, Guid.NewGuid().ToString());
            Directory.CreateDirectory(temp_instance_dir);
        }

        private void finishRestore()
        {
            progress?.Update(RestoreState.Finishing, 0);

            //disconnectSftp(); - provádí se v SftpUploaderFactory
            disconnectSql();
            removeTempDir();

            if (response.errors.Any() && response.Success == SuccessLevel.EverythingWorked)
                response.Success = SuccessLevel.SomeErrors;

            progress?.Update(RestoreState.Done, 0);
        }

        private bool getSftp()
        {
            try
            {
                progress?.Update(RestoreState.ConnectingSftp, 0f);
                sftp = Manager.Get<SftpUploader>(false);
                return true;
            }
            catch (Exception ex)
            {
                bigError("Chyba při připojování k SFTP", ex);
                return false;
            }
        }



        private bool getSql()
        {
            try
            {
                progress?.Update(RestoreState.ConnectingSql, 0f);
                sql = Manager.Get<SqlBackuper>();
                sql.Open();
                return true;
            }
            catch (Exception ex)
            {
                smallError("Problém při připojování k SQL serveru pro obnovu.", ex);
                return false;
            }
        }



        private bool getZip()
        {
            if (Info.location == BackupLocation.Local)
            {
                logInfo("Při obnově se použije lokálně uložený zip");

                if (File.Exists(Info.zip_path))
                {
                    zip_path = Info.zip_path;
                    return true;
                }
                else
                {
                    bigError("Nenašel se zip pro obnovu", null);
                    return false;
                }
            }
            else if (Info.location == BackupLocation.SFTP)
            {
                logInfo("Stahuji zip ze serveru");

                progress?.Update(RestoreState.DownloadingZip, 0, "0 %");

                zip_path = Path.Combine(temp_instance_dir, Path.GetFileName(Info.zip_path));

                try
                {
                    float full_size = sftp.client.Get(Info.zip_path.NormalizePath()).Length;

                    //průběžně budeme posílat GUI info o tom, kolik bytů již bylo staženo.
                    //nechceme to ale posílat moc často, takže to ohlídáme pomocí Stopwatch.
                    Stopwatch progress_interval_limiter = new Stopwatch();
                    progress_interval_limiter.Start();

                    //stáhnout zip ze serveru, průběžně přitom updatovat progress
                    sftp.DownloadFile(Info.zip_path, zip_path, ul =>
                    {
                        if (progress_interval_limiter.ElapsedMilliseconds > Const.UPDATE_GUI_SFTP_UPLOAD_MIN_MS_INTERVAL)
                        {
                            float part = ul / full_size;
                            progress?.Update(RestoreState.DownloadingZip, part, $"{Math.Ceiling(part * 100)} %");
                            progress_interval_limiter.Restart();
                        }
                    });

                    progress_interval_limiter.Stop();
                    return true;
                }
                catch (SftpPathNotFoundException ex)
                {
                    bigError("Na serveru neexistuje zip zálohy pro obnovu, ačkoliv záloha tvrdí opak.", ex);
                    return false;
                }
                catch (Exception ex)
                {
                    bigError("Problém při stahování zipu pro obnovu.", ex);
                    return false;
                }
            }
            else
                throw new NotImplementedException($"? {Info.location}");
        }

        private bool unZip()
        {
            try
            {
                progress?.Update(RestoreState.ExtractingZip, 0);
                src_dir = Path.Combine(temp_instance_dir, "unzipped");
                ZipFile.ExtractToDirectory(zip_path, src_dir);
                return true;
            }
            catch (Exception ex)
            {
                bigError("Problém při extrahování archivu s obnovou", ex);
                return false;
            }
        }

        private bool restoreLocalSourceFromTempLocation(SavedSource src)
        {
            string path = Path.Combine(src_dir, src.filename);
            switch (src.type)
            {
                case BackupSourceType.Database:
                    try
                    {
                        sql.Restore(src.sourcename, path);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        smallError($"Problém při obnově SQL databáze {src.sourcename}", ex);
                        return false;
                    }
                case BackupSourceType.Directory:
                    try
                    {
                        List<string> errorPaths = new List<string>();
                        if (!FileUtils.MoveFolderContentsOverride(path, src.sourcepath, errorPaths))
                            response.errors.Add(
                                new Error(
                                    $"Při obnově složky {src.sourcename} se nepodařilo obnovit {errorPaths.Count} souborů",
                                    string.Join("\n", errorPaths)));
                        return true;
                    }
                    catch (Exception ex)
                    {
                        smallError($"Problém při obnově složky {src.sourcename}", ex);
                        return false;
                    }
                case BackupSourceType.File:
                    try
                    {
                        Directory.CreateDirectory(src.sourcepath.PathMoveUp());
                        if (File.Exists(src.sourcepath))
                            File.Delete(src.sourcepath);
                        File.Move(path, src.sourcepath);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        smallError($"Problém při obnově souboru {src.sourcename}", ex);
                        return false;
                    }
                default:
                    throw new NotImplementedException($"? {src.type}");
            }
        }

        private bool restoreLocalSourceFromPermanentLocation(SavedSource src)
        {
            string from_path = Path.Combine(Info.zip_path, src.filename);
            switch(src.type)
            {
                case BackupSourceType.Database:
                    try
                    {
                        if (Info.location == BackupLocation.SFTP)
                        {
                            string saved_to = Path.Combine(temp_instance_dir, src.filename);
                            sftp.DownloadFile(from_path, saved_to);
                            sql.Restore(src.sourcename, saved_to);
                            File.Delete(saved_to);
                            return true;
                        }
                        else
                        {
                            if (!File.Exists(from_path))
                            {
                                smallError($"Soubor {from_path} pro obnovení zálohy databáze neexistuje.", null);                                
                                return false;
                            }

                            sql.Restore(src.sourcename, from_path);
                            return true;
                        }
                    }
                    catch (Exception ex)
                    {
                        smallError($"Problém při obnově SQL databáze {src.sourcename}", ex);
                        return false;
                    }
                case BackupSourceType.Directory:
                    try
                    {
                        if (Info.location == BackupLocation.SFTP)
                        {
                            sftp.DownloadFolder(from_path, src.sourcepath, FolderUploadBehavior.AddOverwrite);
                            return true;
                        }
                        else
                        {
                            List<string> errorPaths = new List<string>();
                            if (!FileUtils.CopyFolderContents(from_path, src.sourcepath, errorPaths))
                            {
                                smallError($"Při obnově složky {src.sourcename} se nepodařilo obnovit {errorPaths.Count} souborů", null, string.Join("\n", errorPaths));
                                return false;
                            }
                            return true;
                        }
                    }
                    catch (AggregateException agg_ex) //obnova složky může vyhodit AggregateException
                    {
                        foreach(var ex in agg_ex.InnerExceptions)
                            smallError($"Problém při obnově složky {src.sourcename}", ex);
                        return false;
                    }
                    catch (Exception ex)
                    {
                        smallError($"Problém při obnově složky {src.sourcename}", ex);
                        return false;
                    }
                case BackupSourceType.File:
                    try
                    {
                        if (Info.location == BackupLocation.SFTP)
                        {
                            sftp.DownloadFile(from_path, src.sourcepath);
                            return true;
                        }
                        else
                        {
                            File.Copy(from_path, src.sourcepath, true);
                            return true;
                        }
                    }
                    catch (Exception ex)
                    {
                        smallError($"Problém při obnově souboru {src.sourcename}", ex);
                        return false;
                    }
                default:
                    throw new NotImplementedException($"? {src.type}");
            }
        }


        private bool disconnectSql()
        {
            if (sql == null) return true;

            try
            {
                //progress?.Update("ODPOJUJI SE OD SQL", 0.85f);
                if (sql.connection.State == System.Data.ConnectionState.Open)
                    sql.Close();
                sql.connection.Dispose();
                return true;
            }
            catch (Exception ex)
            {
                SmbLog.Error("Problém při odpojování od SQL serveru po obnově", ex, LogCategory.RestoreTask);
                return false;
            }
        }

        private bool removeTempDir()
        {
            //progress?.Update("ODSTRAŇUJI ZBYTKY", 0.9f);

            if (!Directory.Exists(temp_instance_dir)) return true;

            if (!FileUtils.DeleteFolder(temp_instance_dir))
            {
                SmbLog.Error($"Nepodařilo se odstranit některé dočasné soubory po obnově.", null, LogCategory.RestoreTask);
                return false;
            }

            return true;
        }
    }
}
