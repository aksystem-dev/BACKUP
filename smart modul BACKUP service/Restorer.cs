using Renci.SshNet.Common;
using SmartModulBackupClasses;
using SmartModulBackupClasses.Managers;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smart_modul_BACKUP_service
{
    /// <summary>
    /// Provádí obnovy záloh.
    /// </summary>
    [Obsolete("Nepoužívá se. Kód pro provedení obnovy byl přesunut do třídy RestoreTask.")]
    public class Restorer
    {
        public string TempDir;

        //public RestoreResponse restoreZip(Restore restoreInfo, RestoreInProgress progress)
        //{
        //    var bk = Manager.Get<BackupInfoManager>().LocalBackups.FirstOrDefault(bak => bak.LocalID == restoreInfo.backupID);
        //    SMB_Log.Log("Obnova započata");
        //    progress?.Update("STARTUJI OBNOVU", 0);

        //    var R = new RestoreResponse(restoreInfo) { Success = SuccessLevel.EverythingWorked };

        //    //vytvořit složku, kam se extrahuje zip
        //    string temp_instance_dir = Path.Combine(TempDir, "restore" + Directory.GetDirectories(TempDir).Length.ToString());
        //    Directory.CreateDirectory(temp_instance_dir);

        //    SftpUploader sftp = null;
        //}

        public RestoreResponse Restore(Restore restoreInfo, RestoreInProgress progress)
        {
            var bk = Manager.Get<BackupInfoManager>().LocalBackups.FirstOrDefault(bak => bak.LocalID == restoreInfo.backupID);

            DumbLogger.Log("Obnova započata");
            progress.Update("STARTUJI OBNOVU", 0);
            Utils.GUIS.StartRestore(progress);
            progress.AfterUpdateCalled += () => Utils.GUIS.UpdateRestore(progress);

            var R = new RestoreResponse(restoreInfo) { Success = SuccessLevel.EverythingWorked };

            #region LOCATE / DOWNLOAD ZIP FILE

            Directory.CreateDirectory(TempDir);
            string temp_instance_dir = Path.Combine(TempDir, "restore" + Directory.GetDirectories(TempDir).Length.ToString());
            Directory.CreateDirectory(temp_instance_dir);

            string src_path;
            SftpUploader sftp = null;
            try
            {
                if (restoreInfo.location == BackupLocation.SFTP)
                {
                    progress.Update("PŘIPOJUJI SE PŘES SFTP", 0.1f);

                    try
                    {
                        sftp = Manager.Get<SftpUploader>();
                        sftp.Connect();
                    }
                    catch (Exception e)
                    {
                        string msg = $"Došlo k chybě při připojování k sftp ({e.GetType().Name})\n\n{e.Message}";
                        DumbLogger.Ex(e);

                        R.errors.Add(new Error(msg));
                        R.Success = SuccessLevel.TotalFailure;
                        return R;
                    }

                    progress.Update("STAHUJI ZÁLOHU", 0.2f);
                }

                src_path = GetZip(restoreInfo, temp_instance_dir, sftp, R, bk); //může být buďto složka nebo zip
            }
            catch (Exception e)
            {
                DumbLogger.Log("GetZip selhal, vracím R");

                if (sftp?.IsConnected == true)
                    try
                    {
                        sftp.Disconnect();
                    }
                    catch (Exception ex)
                    {
                        DumbLogger.Ex(e);
                    }

                R.Success = SuccessLevel.TotalFailure;
                return R;
            }

            #endregion

            string src_dir; //bude vždy složka

            if (bk.IsZip)
            {
                #region UNZIP
                src_dir = Path.Combine(temp_instance_dir, "unzipped");
                try
                {
                    progress.Update("EXTRAHUJI ZIP", 0.27f);
                    DumbLogger.Log($"Obnova: extrahuju {src_path} do {src_dir}");
                    ZipFile.ExtractToDirectory(src_path, src_dir);
                }
                catch (Exception e)
                {
                    string msg = $"Došlo k chybě při extrahování zip archivu z {src_path} do {src_dir} - {e.GetType().Name}: {e.Message}";
                    DumbLogger.Ex(e);


                    R.errors.Add(new Error(msg));
                    R.Success = SuccessLevel.TotalFailure;
                    return R;
                }
                #endregion
            }
            else
                src_dir = src_path;

            #region SQL CONNECTION INSTANCE

            SqlBackuper sql = null;

            try
            {
                if (restoreInfo.sources.Any(f => f.type == BackupSourceType.Database))
                {
                    progress.Update("PŘIPOJUJI SE K SQL SERVERU", 0.5f);

                    //pokud je mezi zdroji alespoň jedna databáze, vytvoříme SQL připojení
                    sql = Manager.Get<SqlBackuper>();
                    sql.Open();
                }
            }
            catch (Exception e)
            {
                string msg = $"Došlo k chybě k připojivání přes SQL ({e.GetType().Name}): {e.Message}";
                DumbLogger.Ex(e);

                R.Success = SuccessLevel.SomeErrors;
                R.errors.Add(new Error(msg));
            }

            #endregion

            int index = 0;
            float prog_start = 0.55f;
            float prog_end = 0.85f;
            float max_ind = restoreInfo.sources.Length - 1;

            foreach (var source in restoreInfo.sources)
            {
                progress.Update($"OBNOVUJI ZDROJ {source.sourcename}", SMB_Utils.Lerp(prog_start, prog_end, max_ind == 0 ? 0 : index / max_ind));

                #region RESTORE SOURCE

                string unzipped_path = Path.Combine(src_dir, source.filename);

                SuccessLevel success = SuccessLevel.EverythingWorked;

                switch (source.type)
                {
                    case BackupSourceType.Database:
                        try
                        {
                            sql.Restore(source.sourcepath, unzipped_path);
                        }
                        catch (Exception e)
                        {
                            string msg = $"Problém s obnovením SQL databáze {source.sourcepath} ({e.GetType().Name}): {e.Message}";
                            DumbLogger.Ex(e);

                            R.errors.Add(new Error(msg));
                            success = SuccessLevel.TotalFailure;
                        }
                        break;
                    case BackupSourceType.Directory:
                        try
                        {
                            //if (Directory.Exists(source.sourcepath))
                            //{
                            //    Directory.Delete(source.sourcepath, true);
                            //    Directory.CreateDirectory(source.sourcepath);
                            //}
                            if (!FileUtils.CopyFolderContents(unzipped_path, source.sourcepath))
                                success = SuccessLevel.SomeErrors;
                        }
                        catch (Exception e)
                        {
                            string msg = $"Problém s obnovením adresáře {source.sourcepath} ({e.GetType().Name}): {e.Message}";
                            DumbLogger.Ex(e);

                            R.errors.Add(new Error(msg));
                            success = SuccessLevel.TotalFailure;
                        }
                        break;
                    case BackupSourceType.File:
                        try
                        {
                            Directory.CreateDirectory(source.sourcepath.PathMoveUp());
                            File.Copy(unzipped_path, source.sourcepath, true);
                        }
                        catch (Exception e)
                        {
                            string msg = $"Problém s obnovením souboru {source.sourcepath} ({e.GetType().Name}): {e.Message}";
                            DumbLogger.Ex(e);

                            R.errors.Add(new Error(msg));
                            success = SuccessLevel.TotalFailure;
                        }

                        break;
                }

                if (success == SuccessLevel.EverythingWorked)
                    DumbLogger.Success($"Zdroj {source.sourcepath} (typ {source.type}) úspěšně obnoven");
                else
                    DumbLogger.Success($"Během obnovy zdroje {source.sourcepath} (typ {source.type}) došlo k chybám");
                source.Success = success;

                #endregion

                index++;
            }

            try
            {
                progress.Update("ODPOJUJI SE OD SQL SERVERU", 0.9f);
                sql?.Close();
                progress.Update("ODPOJUJI SE OD SFTP SERVERU", 0.92f);
                sftp?.Disconnect();
            }
            catch (Exception e)
            {
                DumbLogger.Warn($"{e.GetType().Name}: \n\n{e.Message}");
            }

            try
            {
                progress.Update("ODSTRAŇUJI DOČASNOU SLOŽKU", 0.95f);
                DumbLogger.Log("Odstraňuji dočasnou složku...");
                Directory.Delete(temp_instance_dir, true);
            }
            catch (Exception e)
            {
                DumbLogger.Ex(e);

            }

            if (R.info.sources.Any(f => f.Success != SuccessLevel.EverythingWorked))
                R.Success = SuccessLevel.SomeErrors;

            if (R.Success == SuccessLevel.EverythingWorked)
            {
                DumbLogger.Success($"Úspěšně dokončena obnova.");
                progress.Update("OBNOVA ÚSPŠNĚ DOKONČENA", 1);
            }
            else
            {
                DumbLogger.Warn($"Obnova dokončena, ale došlo k chybám.");
                progress.Update("OBNOVA DOKONČENA, ALE DOŠLO K CHYBÁM", 1);
            }

            Utils.GUIS.CompleteRestore(progress, R);

            return R;
        }

        /// <summary>
        /// Pokud je záloha lokální, vrátí, kde je uložený zip zálohy popř. složka zálohy. Jinak zip / složku stáhne a vrátí, kam to stáhnul.
        /// </summary>
        /// <param name="r"></param>
        /// <param name="workdir"></param>
        /// <returns></returns>
        private string GetZip(Restore r, string workdir, SftpUploader sftp = null, RestoreResponse response = null, Backup bk = null)
        {
            DumbLogger.Log("GetZip zavoláno");
            bk = bk ?? Manager.Get<BackupInfoManager>().LocalBackups.FirstOrDefault(b => b.LocalID == r.backupID);

            //je-li záloha lokální, prostě vrátíme lokální umístění zipu
            if (r.location == BackupLocation.Local)
            {
                DumbLogger.Log("Při obnově se použije lokálně uložený zip");

                bool exists = bk.IsZip ? File.Exists(r.zip_path) : Directory.Exists(r.zip_path);
                if (!exists)
                {
                    string msg = $"Lokální zip soubor nebyl nalezen (měl být na adrese {r.zip_path}, ale zdá se, že není)";
                    DumbLogger.Error(msg);
                    response?.errors.Add(new Error(msg));
                    throw new Exception();
                }

                return r.zip_path;
            }

            //je-li na serveru, zip stáhneme a vrátíme cestu, kam jsme ho stáhli
            else if (r.location == BackupLocation.SFTP)
            {
                DumbLogger.Log("Při obnově se použije záloha na vzdáleném serveru");

                string zip_path = Path.Combine(workdir, Path.GetFileName(r.zip_path));

                //if (!sftp.client.Exists(r.zip_path))
                //{
                //    string msg = $"Na serveru není zip uložen (měl být na adrese {r.zip_path}, ale zdá se, že není)";
                //    Logger.Error(msg);
                //    response?.errors.Add(new Error(msg));
                //    throw new Exception();
                //}

                try
                {
                    DumbLogger.Log($"Stahuju zálohu přes SFTP {r.zip_path} do souboru {zip_path}");
                    //using (var writer = File.OpenWrite(zip_path))
                    //    sftp.client.DownloadFile(r.zip_path.FixPathForSFTP(), writer);

                    if (bk.IsZip)
                        sftp.DownloadFile(r.zip_path, zip_path);
                    else
                        sftp.DownloadFolder(r.zip_path, zip_path, FolderUploadBehavior.AddOverwrite);
                }
                catch (SftpPathNotFoundException e)
                {
                    string msg = $"Soubor {r.zip_path.FixPathForSFTP()} nebyl nalezen na serveru";
                    DumbLogger.Ex(e);

                    response?.errors.Add(new Error(msg));
                    throw e;
                }
                catch (Exception e)
                {
                    string msg = $"Při stahování zipu došlo k výjimce {e.GetType().Name}: {e.Message}";
                    DumbLogger.Ex(e);

                    response?.errors.Add(new Error(msg));
                    throw e;
                }

                return zip_path;
            }

            else
                throw new NotImplementedException($"Nepodporuji {r.location}");
        }
    }
}
