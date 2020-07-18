using Renci.SshNet;
using Renci.SshNet.Common;
using smart_modul_BACKUP_service.BackupExe;
using SmartModulBackupClasses;
using SmartModulBackupClasses.Managers;
using System;
using System.Collections.Generic;
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

            bkToRestore = Manager.Get<BackupInfoManager>().LocalBackups.FirstOrDefault(b => b.LocalID == Info.backupID);
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
            progress?.Update("STARTUJI OBNOVU", 0);
            logInfo("Obnova složky započata;");

            Thread.Sleep(1500);

            response = new RestoreResponse(Info) { Success = SuccessLevel.EverythingWorked };

            Directory.CreateDirectory(TEMP_DIR);
            temp_instance_dir = Path.Combine(TEMP_DIR, "restore" + Directory.GetDirectories(TEMP_DIR).Length.ToString());
            Directory.CreateDirectory(temp_instance_dir);

            if (Info.location == BackupLocation.SFTP)
                if (!getSftp()) goto finish;

            if (Info.sources.Any(src => src.type == BackupSourceType.Database))
                getSql();

            foreach(var src in Info.sources)
                restoreLocalSourceFromPermanentLocation(src);

        finish:
            finishRestore();
            return response;
        }

        private RestoreResponse restoreZip()
        {
            progress?.Update("STARTUJI OBNOVU", 0);
            logInfo("Obnova zipu započata.");

            Thread.Sleep(1500);

            response = new RestoreResponse(Info) { Success = SuccessLevel.EverythingWorked };

            Directory.CreateDirectory(TEMP_DIR);
            temp_instance_dir = Path.Combine(TEMP_DIR, "restore" + Directory.GetDirectories(TEMP_DIR).Length.ToString());
            Directory.CreateDirectory(temp_instance_dir);

            if (Info.location == BackupLocation.SFTP)
                if (!getSftp()) goto finish;

            if (Info.sources.Any(src => src.type == BackupSourceType.Database))
                getSql();

            if (!getZip()) goto finish;
            if (!unZip()) goto finish;

            foreach (var src in Info.sources)
                restoreLocalSourceFromTempLocation(src);

        finish:
            finishRestore();
            return response;
        }

        private void finishRestore()
        {
            disconnectSftp();
            disconnectSql();
            removeTempDir();

            if (response.errors.Any() && response.Success == SuccessLevel.EverythingWorked)
                response.Success = SuccessLevel.SomeErrors;
        }

        private bool getSftp()
        {
            try
            {
                sftp = Manager.Get<SftpUploader>();
                sftp.Connect();
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

                zip_path = Path.Combine(temp_instance_dir, Path.GetFileName(Info.zip_path));

                try
                {
                    sftp.DownloadFile(Info.zip_path, zip_path);
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

        private bool disconnectSftp()
        {
            if (sftp == null) return true;

            try
            {
                if (sftp.IsConnected)
                    sftp.Disconnect();
                sftp.Dispose();
                return true;
            }
            catch (Exception ex)
            {
                SmbLog.Error("Problém při odpojování od SFTP serveru po obnově.", ex, LogCategory.RestoreTask);
                return false;
            }
        }

        private bool disconnectSql()
        {
            if (sql == null) return true;

            try
            {
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
