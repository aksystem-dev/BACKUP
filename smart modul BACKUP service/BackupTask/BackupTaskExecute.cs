using SmartModulBackupClasses;
using SmartModulBackupClasses.Managers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smart_modul_BACKUP_service.BackupExe
{
    public partial class BackupTask
    {
        const string TEMP_DIR = "temp_dir";
        private Backup B_Obj = null;
        private SftpUploader sftp = null;
        private SqlBackuper sql = null;
        private Dictionary<string, VssBackuper> shadowCopies = new Dictionary<string, VssBackuper>();
        private Config cfg;
        private string temp_instance_dir; //složka používaná tímto konkrétním vyhodnocením

        private async Task backup()
        {
            Utils.GUIS.StartBackup(Progress);

            if (Progress != null)
                Progress.AfterUpdateCalled += () => Utils.GUIS.UpdateBackup(Progress);
            Progress?.Update("INICIALIZACE", 0);

            lock (RunningBackupTasks)
                RunningBackupTasks.Add(this);

            cfg = Manager.Get<ConfigManager>().Config;

            //Vytvořit objekt s informacemi o záloze
            B_Obj = new Backup()
            {
                RefRule = Rule.LocalID,
                RefRuleName = Rule.Name,
                Errors = new List<BackupError>(),
                Sources = new List<SavedSource>(),
                Success = true,
                StartDateTime = DateTime.Now,
                ComputerId = SMB_Utils.GetComputerId(),
                Saved = false,
                IsZip = Rule.Zip
            };
            await Manager.Get<BackupInfoManager>().AddQuietlyAsync(B_Obj);

            if (IsCancelled) goto finish;
            Progress?.Update("SPOUŠTÍM PROCESY", 0.05f);
            if (!runProcesses()) goto finish;
            
            if (IsCancelled) goto finish;
            if (!createTempDir()) goto finish;

            if (IsCancelled) goto finish;
            Progress?.Update("PŘIPOJUJI SFTP", 0.15f);
            connectSftp();

            if (IsCancelled) goto finish;
            Progress?.Update("PŘIPOJUJI SQL", 0.2f);
            connectSql();

            if (IsCancelled) goto finish;
            Progress?.Update("VYTVÁŘÍM SHADOW COPY", 0.25f);
            vss();

            Progress?.Update("ZÁLOHUJI ZDROJE", 0.35f);
            string bk_dir = Path.Combine(temp_instance_dir, "dir");
            foreach(var src in Rule.Sources.All.Where(s => s.enabled))
            {
                if (IsCancelled) goto finish;
                var saved = backupSource(src, bk_dir);
                B_Obj.Sources.Add(saved);
            }

            if (IsCancelled) goto finish;
            string bk_path = bk_dir;
            if (Rule.Zip)
            {
                Progress?.Update("ZIPUJI ZÁLOHU", 0.5f);
                if (!zip(bk_dir, out bk_path))
                    goto finish;
            }

            string bk_fname = $"{Rule.Name}_{B_Obj.LocalID.ToString()}_{DateTime.Now.ToString("dd-MM-yyyy")}";

            try
            {
                B_Obj.Size = Rule.Zip ? new FileInfo(bk_path).Length : FileUtils.GetDirSize(bk_path);
            }
            catch (Exception ex)
            {
                SMB_Log.LogEx(ex, "Chyba při měření velikosti zálohy");
                error("Nepodařilo se změřit velikost zálohy");
            }

            if (IsCancelled) goto finish;
            if (Rule.RemoteBackups.enabled)
            {
                Progress?.Update("NAHRÁVÁM PŘES SFTP", 0.6f);
                uploadFile(bk_path, bk_fname);
            }

            if (IsCancelled) goto finish;
            if (Rule.LocalBackups.enabled)
            {
                Progress?.Update("PŘESOUVÁM DO MÍSTNÍ SLOŽKY", 0.7f);
                saveFile(bk_path, bk_fname);
            }

        finish:
            if (IsCancelled)
                error("Záloha byla zrušena.");

            Progress?.Update(IsCancelled ? "RUŠÍM ZÁLOHU" : "UKONČUJI ZÁLOHU", 0.8f);
            await finishBackup();
        }

        private void error(string error, BackupErrorType type = BackupErrorType.DefaultError, bool showBubble = false)
        {
            SMB_Log.Error(error, 5);

            try
            {
                B_Obj.Errors.Add(new BackupError(error, type));
                B_Obj.Success = false;
            }
            catch (NullReferenceException) { }

            if (showBubble)
                try
                {
                    Utils.GUIS.ShowError(error);
                }
                catch { }
        }

        private bool createTempDir()
        {
            try
            {
                Directory.CreateDirectory(TEMP_DIR); //složka pro ukládání dočasných dat; ujistit se, že existuje
                temp_instance_dir = Path.Combine(TEMP_DIR, "backup" + Directory.GetDirectories(TEMP_DIR).Length.ToString());
                Directory.CreateDirectory(temp_instance_dir); //vytvořit složku pro toto vyhodnocení
                return true;
            }
            catch (Exception ex)
            {
                SMB_Log.LogEx(ex);
                error("Nepodařilo se vytvořit složku pro vyhodnocení pravidla.");
                return false;
            }
        }

        /// <summary>
        /// Spustí procesy, které se mají spustit před vyhodnocením pravidla.
        /// </summary>
        /// <returns>Pokud false, vykašlat se na zálohu. Pokud true, vše v pořádku</returns>
        private bool runProcesses()
        {
            foreach (var pars in Rule.ProcessesBeforeStart)
            {
                try
                {
                    var process = new Process();
                    process.StartInfo = new ProcessStartInfo()
                    {
                        FileName = pars.ProcessName,
                        Arguments = pars.Arguments,
                        RedirectStandardInput = false,
                        RedirectStandardOutput = false,
                        CreateNoWindow = false,
                        WindowStyle = ProcessWindowStyle.Normal,
                        UseShellExecute = true
                    };
                    if (!process.Start())
                        throw new Exception("Proces se nepodařilo spustit.");
                    if (!process.WaitForExit(pars.Timeout))
                        throw new Exception("Časový limit čekání na ukončení procesu vypršel.");
                }
                catch (Exception ex)
                {
                    error($"Nepodařilo se úspěšně dokončit proces {Path.GetFileName(pars.ProcessName)} před spuštěním pravidla. {ex.Message}");

                    if (pars.Require)
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Nastaví instanci SftpUploader a připojí se (pouze pokud je třeba)
        /// </summary>
        /// <returns>Zdali je vše v poho.</returns>
        private bool connectSftp()
        {
            if (Rule.RemoteBackups.enabled && Rule.RemoteBackups.MaxBackups > 0)
            {
                SMB_Log.Log("Připojuji se k SFTP serveru");
                try
                {
                    sftp = Manager.Get<SftpUploader>();
                    sftp.Connect();
                    SMB_Log.Log("Úspěšně připojeno k SFTP serveru");

                    return true;
                }
                catch (Exception e)
                {
                    SMB_Log.LogEx(e);
                    error("Službě smart modul BACKUP se nepodařilo připojit ke vzdálenému úložišti.", BackupErrorType.SftpError);

                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Nastaví instanci SqlBackuper a připojí se (pouze pokud je třeba)
        /// </summary>
        /// <returns>Zdali je vše v poho.</returns>
        private bool connectSql()
        {
            if (Rule.Sources.Databases.Any())
            {
                SMB_Log.Log("Připojuji se přes SQL");
                try
                {
                    sql = Manager.Get<SqlBackuper>();
                    sql.Open();
                    SMB_Log.Log("Úspěšně připojeno k SQL serveru");
                    return true;
                }
                catch (Exception e)
                {
                    SMB_Log.LogEx(e);
                    error("Službě smart modul BACKUP se nepodařilo připojit k SQL serveru.");
                    sql = null;
                    return false;
                }
            }
            return true;

        }

        /// <summary>
        /// Pokud se mají využívat ShadowCopy, vytvoří ShadowCopy.
        /// </summary>
        /// <returns>Zdali je vše v poho.</returns>
        private bool vss()
        {
            if (cfg.UseShadowCopy)
            {
                SMB_Log.Log("Vytvářím Shadow Copy");

                foreach (var src in Rule.Sources.Directories)
                {
                    string root = Path.GetPathRoot(src.path);
                    if (!shadowCopies.ContainsKey(root)) //pokud jsme pro tento svazek ještě Shadow Copy nevytvořili
                    {
                        try
                        {
                            SMB_Log.Log($"Chystám se na Shadow Copy svazku {root}");

                            var shadowCopy = new VssBackuper();
                            shadowCopy.DoBackup(root, B_Obj.Errors);
                            shadowCopies.Add(root, shadowCopy);

                            SMB_Log.Log($"Shadow Copy svazku {root} vytvořena");
                        }
                        catch (Exception ex)
                        {
                            error($"Nepodařilo se vytvořit shadow copy svazku {root}");
                            SMB_Log.LogEx(ex);
                        }
                    }
                }

                Logger.Log("Shadow Copy vytvořeny");
            }

            return true;
        }

        /// <summary>
        /// Provede zálohu konkrétního zdroje.
        /// </summary>
        /// <returns>Objekt SavedSource s informacemi o záloze zdroje.</returns>
        private SavedSource backupSource(BackupSource source, string dir)
        {
            Logger.Log($"Vytvářím zálohu zdroje {source.id}");

            var saved = new SavedSource()
            {
                type = source.type,
                Success = SuccessLevel.EverythingWorked,
                sourcepath = source.path
            };

            try
            {
                if (source.type == BackupSourceType.Database)
                {
                    //zde porychtujeme databáze

                    //pokud nemáme instanci SqlBackuperu (např. se připojení nezdařilo), kašlem na to
                    if (sql == null)
                    {
                        saved.Error = "Nelze zálohovat databázi, neboť se nepodařilo připojit k SQL serveru.";
                        saved.ErrorDetail = null;
                        saved.Success = SuccessLevel.TotalFailure;
                        return saved;
                    }

                    //sestavit cestu k záloze
                    saved.filename = source.id + ".bak";
                    string bak_path = Path.Combine(dir, saved.filename);

                    //zálohovat databázi
                    try
                    {
                        sql.Backup(source.path, bak_path);
                        File.SetAttributes(bak_path, FileAttributes.Normal);
                        saved.Success = SuccessLevel.EverythingWorked;
                        return saved;
                    }
                    catch (Exception ex)
                    {
                        SMB_Log.LogEx(ex, $"Problém při záloze zdroje {source.id}");
                        saved.Success = SuccessLevel.TotalFailure;
                        saved.Error = ex.Message;
                        return saved;
                    }
                }
                else if (source.type == BackupSourceType.Directory)
                {
                    //zde porychtujeme složky

                    string root = Path.GetPathRoot(source.path);

                    //sestavit cestu k záloze
                    saved.filename = source.id;
                    string dir_path = Path.Combine(dir, saved.filename);

                    List<string> failed_paths = new List<string>();

                    bool copied_all = FileUtils.CopyFolderContents(
                        //pokud máme pro tento volume Shadow Copy, budeme zipovat Shadow Copy; jinak normálně ten soubor
                        shadowCopies.ContainsKey(root) ? shadowCopies[root].GetShadowPath(source.path) : source.path,
                        dir_path, failed_paths);

                    if (copied_all)
                    {
                        saved.Success = SuccessLevel.EverythingWorked;
                        return saved;
                    }
                    else
                    {
                        saved.Success = SuccessLevel.SomeErrors;
                        saved.Error = "Nepodařilo se zkopírovat některé soubory.";
                        saved.ErrorDetail = String.Join("\n", failed_paths);
                        return saved;
                    }
                }
                else if (source.type == BackupSourceType.File)
                {
                    //zde porychtujeme soubory

                    string root = Path.GetPathRoot(source.path);

                    //sestavit cestu k záloze
                    saved.filename = source.id + Path.GetExtension(source.path);
                    string file_path = Path.Combine(dir, saved.filename);

                    try
                    {
                        File.Copy(
                            //pokud máme pro tento volume Shadow Copy, budeme zipovat Shadow Copy; jinak normálně ten soubor
                            shadowCopies.ContainsKey(root) ? shadowCopies[root].GetShadowPath(source.path) : source.path,
                            file_path, true);

                        saved.Success = SuccessLevel.EverythingWorked;
                        saved.Error = null;
                        saved.ErrorDetail = null;
                        return saved;
                    }
                    catch (Exception ex)
                    {
                        SMB_Log.LogEx(ex, $"Problém při záloze zdroje {source.id}");
                        saved.Success = SuccessLevel.TotalFailure;
                        saved.Error = ex.Message;
                        saved.ErrorDetail = $"Výjimka {ex.GetType().Name}:\n{ex.Message}";
                        return null;
                    }
                }
                else
                {
                    saved.Success = SuccessLevel.TotalFailure;
                    saved.Error = $"Neznám typ zdroje {source.type}";
                    saved.ErrorDetail = null;
                    return saved;
                }
                //throw new ArgumentException($"Neznám typ zdroje {source.type}.");
            }
            catch (Exception ex)
            {
                SMB_Log.LogEx(ex);

                //pokud dojde k výjimce, zpráva z výjimky se uloží jako chyba
                saved.Error = "Došlo k neošetřené výjimce";
                saved.ErrorDetail = $"Výjimka {ex.GetType().Name}:\n{ex.Message}";
                saved.Success = SuccessLevel.TotalFailure;
                return saved;
            }
        }

        /// <summary>
        /// Zapipuje source_dir a vrátí cestu k zipu.
        /// </summary>
        /// <param name="source_dir"></param>
        /// <param name="zip_path"></param>
        /// <returns>Zdali je vše v poho.</returns>
        private bool zip(string source_dir, out string zip_path)
        {
            try
            {
                SMB_Log.Log("Zipuji zálohu");
                zip_path = Path.Combine(temp_instance_dir, "temp.zip");
                ZipFile.CreateFromDirectory(source_dir, zip_path);
                return true;
            }
            catch (Exception ex)
            {
                SMB_Log.LogEx(ex);
                error("Nepodařilo se zazipovat zdroj.");
                zip_path = null;
                return false;
            }
        }

        /// <summary>
        /// Nahraje soubor na server na správné umístění pod daným jménem
        /// </summary>
        /// <param name="src"></param>
        /// <param name="fname"></param>
        /// <returns></returns>
        private bool uploadFile(string src, string fname)
        {
            if (sftp == null) return false;

            try
            {
                string remote_path = Path.Combine(SMB_Utils.GetRemoteBackupPath(), Rule.Name, fname);

                SMB_Log.Log($"Nahrávám {src} přes sftp do {remote_path}");

                if (Rule.Zip)
                    sftp.UploadFile(src, remote_path);
                else
                    sftp.UploadDirectory(src, remote_path, FolderUploadBehavior.ReplaceWhole);

                B_Obj.AvailableRemotely = true;
                B_Obj.RemotePath = remote_path;
                return true;
            }
            catch(Exception ex)
            {
                SMB_Log.LogEx(ex);
                B_Obj.Success = false;
                B_Obj.AvailableRemotely = false;
                error($"Problém s nahráváním souboru přes SFTP na server: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// P5esune zálohu do lokálního umístění
        /// </summary>
        /// <param name="src"></param>
        /// <param name="fname"></param>
        /// <returns></returns>
        private bool saveFile(string src, string fname)
        {
            string rule_folder = Path.Combine(cfg.LocalBackupDirectory, Rule.Name);
            Directory.CreateDirectory(rule_folder);
            string this_bk_path = Path.Combine(rule_folder, fname);

            try
            {
                if (Rule.Zip)
                    File.Move(src, this_bk_path);
                else
                    Directory.Move(src, this_bk_path);

                B_Obj.AvailableLocally = true;
                B_Obj.LocalPath = this_bk_path;
                return true;
            }
            catch (Exception ex)
            {
                SMB_Log.LogEx(ex);
                error($"Nepodařilo se uložit zálohu lokálně. Problém: {ex.Message}");
                return false;
            }
        }

        private async Task finishBackup()
        {
            await saveInfo();

            disconnectSftp();
            disconnectSql();
            cleanUpVss();
            deleteTempFolder();

            if (B_Obj.Success)
                SMB_Log.Log($"Pravidlo {Rule.Name} úspěšně uplatněno.");
            else
                SMB_Log.Warn($"Během vyhodnocování pravidla {Rule.Name} došlo k chybám.");

            Progress.Update("HOTOVO", 1);
            Utils.GUIS.CompleteBackup(Progress, B_Obj.LocalID);
        }

        private async Task<bool> saveInfo()
        {
            try
            {
                B_Obj.EndDateTime = DateTime.Now;
                await Manager.Get<BackupInfoManager>().AddAsync(B_Obj);
                return true;
            }
            catch (Exception ex)
            {
                SMB_Log.LogEx(ex);
                return false;
            }
        }

        private bool disconnectSftp()
        {
            SMB_Log.Log("Odpojuji se od sftp");
            if (sftp != null && sftp.IsConnected)
            {
                try
                {
                    sftp.Disconnect();
                    return true;
                }
                catch (Exception ex)
                {
                    SMB_Log.LogEx(ex);
                    return false;
                }
            }

            return true;
        }

        private bool disconnectSql()
        {
            SMB_Log.Log("Odpojuji se od sql");
            if (sql != null && sql.connection.State == System.Data.ConnectionState.Open)
            {
                try
                {
                    sql.Close();
                    return true;
                }
                catch (Exception ex)
                {
                    SMB_Log.LogEx(ex);
                    return false;
                }

            }

            return false;
        }

        private void cleanUpVss()
        {
            SMB_Log.Log("Odstraňuji shadow copy");
            foreach(var pair in shadowCopies)
            {
                try
                {
                    pair.Value.Dispose();
                }
                catch (Exception ex)
                {
                    SMB_Log.LogEx(ex);
                }
            }
        }

        private bool deleteTempFolder()
        {
            if (temp_instance_dir != null)
            {
                try
                {
                    SMB_Log.Log("Odstraňuji dočasnou složku");
                    return FileUtils.DeleteFolder(temp_instance_dir);
                }
                catch (Exception ex)
                {
                    SMB_Log.LogEx(ex, "Problém při odstraňování dočasné složky");
                    return false;
                }
            }

            return true;
        }
    }
}
