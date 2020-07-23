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
        /// <summary>
        /// Složka, kam se ukládají dočasné soubory při zálohách
        /// </summary>
        const string TEMP_DIR = "temp_dir";

        /// <summary>
        /// Objekt s informacemi o probíhající záloze; ten se poté přes BackupInfoManager serializuje do
        /// xml a uloží do souboru, popř. odešle přes webové api, popř. odešle jako dodatečné info přes sftp na server
        /// </summary>
        private Backup B_Obj = null;

        /// <summary>
        /// Využívaný nahravač dat na sftp server; načte se pomocí connectSftp()
        /// </summary>
        private SftpUploader sftp = null;

        /// <summary>
        /// Využívaný zálohovač SQL databází; načte se pomocí connectSql();
        /// </summary>
        private SqlBackuper sql = null;

        /// <summary>
        /// ShadowCopy, které má tato záloha k dispozici; pokud je vytváření shadow copy
        /// povoleno, vytvoří se pomocí vss()
        /// </summary>
        private Dictionary<string, VssBackuper> shadowCopies = new Dictionary<string, VssBackuper>();

        /// <summary>
        /// Konfigurace
        /// </summary>
        private Config cfg;

        /// <summary>
        /// složka, kam se ukládají dočasná data tohoto konkrétního vyhodnocení; podsložka TEMP_DIR
        /// </summary>
        private string temp_instance_dir;

        /// <summary>
        /// <para>Provede úplnou zálohu do cílových umístění</para>
        /// Tzn. že do cílových složek uloží úplně novou zálohu, takže jich tam může být víc.
        /// </summary>
        /// <returns></returns>
        private async Task backupFull()
        {
            Progress?.Update(BackupState.Initializing, 0);

            cfg = Manager.Get<ConfigManager>().Config;

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
                SftpHash = Rule.RemoteBackups.enabled ? SMB_Utils.GetSftpHash() : null,
                PlanId = SMB_Utils.GetCurrentPlanId()
            };
            await Manager.Get<BackupInfoManager>().AddQuietlyAsync(B_Obj);

            //toto se volá pravidelně v místech, kde je bezpečné zrušit zálohu
            if (IsCancelled) goto finish;

            //spustit procesy, které se mají vyhodnotit před spuštěním tohoto pravidla
            Progress?.Update(BackupState.RunningProcesses, 0);
            if (!runProcesses())
                //runProcesses vrátí false, pokud dojde k závěru, že toto pravidlo by se nemělo vyhodnotit
                //tj. pokud je pravidlo nakonfigurováno tak, že se nesmí spustit pokud se určitý proces
                //nepodaří spustit úspěšně; pokud vrátí false, chceme se na zálohu vykašlat
                goto finish;

            //vytvořit dočasnou složku, kam se budou ukládat dočasná data
            if (IsCancelled) goto finish;
            if (!createTempDir()) goto finish;

            //připojit se přes SFTP na server s daty
            //   (connectSftp je chytrej, takže to udělá pouze, pokud to je třeba)
            if (IsCancelled) goto finish;
            Progress?.Update(BackupState.ConnectingSftp, 0);
            connectSftp();

            //připojit se přes SQL na databázi
            //   (connectSql je chytrej, takže to udělá pouze, pokud to je třeba)
            if (IsCancelled) goto finish;
            Progress?.Update(BackupState.ConnectingSql, 0);
            connectSql();

            //vytvořit shadow copy
            //   (vss je chytrej, takže to udělá pouze, pokud jsou Shadow Copy povoleny)
            if (IsCancelled) goto finish;
            Progress?.Update(BackupState.CreatingVss, 0);
            vss();

            //vytvořit složku, kam se budou zálohocat jednotlivé zdroje
            string bk_dir = Path.Combine(temp_instance_dir, "dir");
            Directory.CreateDirectory(bk_dir);

            //průběh zálohování zdrojů - inicializovat proměnné
            float src_count = Rule.Sources.All.Count;
            int curr = 0;

            //projít povolené zdroje a provést jejich zálohu
            foreach(var src in Rule.Sources.All.Where(s => s.enabled))
            {
                if (IsCancelled) goto finish;
                Progress?.Update(BackupState.BackupSources, curr / src_count, src.path);
                var saved = backupSource(src, bk_dir);
                B_Obj.Sources.Add(saved);
                curr++;
            }

            if (IsCancelled) goto finish;

            //bk_path bude cesta k souboru nebo složce, který/ou budeme chtít nahrát přes SFTP
            //a uložit do lokálního úložiště
            string bk_path = bk_dir;

            //pokud se pravidlo má zazipovat,
            if (Rule.Zip)
            {
                Progress?.Update(BackupState.ZipBackup, 0);
                if (!zip(bk_dir, out bk_path)) //do bk_path se narve cesta k zip souboru
                    goto finish; //pokud se nezdaří, vykašlem se na to
            }

            //název souboru se zálohou
            string bk_fname = $"{Rule.Name}_{B_Obj.LocalID.ToString()}_{DateTime.Now.ToString("dd-MM-yyyy")}" + (Rule.Zip ? ".zip" : "");
            
            //zde změříme velikost souboru a uložíme jí do Backup objektu
            try
            {
                B_Obj.Size = Rule.Zip ? new FileInfo(bk_path).Length : FileUtils.GetDirSize(bk_path);
            }
            catch (Exception ex)
            {
                error("Nepodařilo se změřit velikost zálohy");
            }

            if (IsCancelled) goto finish;

            //pokud jsou povoleny zálohy přes sftp, nahrát bk_path na sftp
            if (Rule.RemoteBackups.enabled)
            {
                Progress?.Update(BackupState.SftpUpload, 0f);
                uploadFile(bk_path, bk_fname);
            }

            if (IsCancelled) goto finish;

            //pokud jsou povoleny zálohy lokální, přesunout bk_path do cílové složky
            if (Rule.LocalBackups.enabled)
            {
                Progress?.Update(BackupState.MovingToLocalFolder, 0.7f);
                saveFile(bk_path, bk_fname);
            }

        finish:
            //zde přijde to, co se má vyhodnotit vždy, i pokud je záloha zrušena
            //čilivá: odstranění bordelu, odpojení od SFTP a SQL, apod.

            if (IsCancelled)
                error("Záloha byla zrušena.");

            Progress?.Update(IsCancelled ? BackupState.Cancelling : BackupState.Finishing, 0.8f);
            await finishBackup();
        }

        private static void logError(string error, Exception ex)
        {
            SmbLog.Error(error, ex, LogCategory.BackupTask);
        }

        private void error(string error, Exception ex = null, BackupErrorType type = BackupErrorType.DefaultError, bool showBubble = false)
        {
            logError(error, ex);

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

        private static void logInfo(string message)
        {
            SmbLog.Info(message, category: LogCategory.BackupTask);
        }

        private bool createTempDir()
        {
            try
            {
                Directory.CreateDirectory(TEMP_DIR); //složka pro ukládání dočasných dat; ujistit se, že existuje

                //složka pro toto konkrétní vyhodnocení
                temp_instance_dir = Path.Combine(TEMP_DIR, Guid.NewGuid().ToString());

                Directory.CreateDirectory(temp_instance_dir); //vytvořit složku pro toto vyhodnocení
                return true;
            }
            catch (Exception ex)
            {
                error("Nepodařilo se vytvořit složku pro vyhodnocení pravidla.", ex);
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
                logInfo("Připojuji se k SFTP serveru");
                try
                {
                    sftp = Manager.Get<SftpUploader>();
                    sftp.Connect();
                    logInfo("Úspěšně připojeno k SFTP serveru");

                    return true;
                }
                catch (Exception e)
                {
                    error("Službě smart modul BACKUP se nepodařilo připojit ke vzdálenému úložišti.", e, BackupErrorType.SftpError);

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
                logInfo("Připojuji se přes SQL");
                try
                {
                    sql = Manager.Get<SqlBackuper>();
                    sql.Open();
                    logInfo("Úspěšně připojeno k SQL serveru");
                    return true;
                }
                catch (Exception e)
                {
                    error("Službě smart modul BACKUP se nepodařilo připojit k SQL serveru.", e);
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
                logInfo("Vytvářím Shadow Copy");

                foreach (var src in Rule.Sources.Directories)
                {
                    string root = Path.GetPathRoot(src.path);
                    if (!shadowCopies.ContainsKey(root)) //pokud jsme pro tento svazek ještě Shadow Copy nevytvořili
                    {
                        try
                        {
                            logInfo($"Chystám se na Shadow Copy svazku {root}");

                            var shadowCopy = new VssBackuper();
                            shadowCopy.DoBackup(root, B_Obj.Errors);
                            shadowCopies.Add(root, shadowCopy);

                            logInfo($"Shadow Copy svazku {root} vytvořena");
                        }
                        catch (Exception ex)
                        {
                            error($"Nepodařilo se vytvořit shadow copy svazku {root}", ex);
                        }
                    }
                }

                logInfo("Shadow Copy vytvořeny");
            }

            return true;
        }

        /// <summary>
        /// Provede zálohu konkrétního zdroje.
        /// </summary>
        /// <returns>Objekt SavedSource s informacemi o záloze zdroje.</returns>
        private SavedSource backupSource(BackupSource source, string dir)
        {
            logInfo($"Vytvářím zálohu zdroje {source.id}");

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
                        error($"Problém při záloze zdroje {source.id}", ex);
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
                        logError($"Problém při záloze zdroje {source.id}", ex);
                        saved.Success = SuccessLevel.TotalFailure;
                        saved.Error = ex.Message;
                        saved.ErrorDetail = $"Výjimka {ex.GetType().Name}:\n{ex.Message}";
                        return saved;
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
                logError($"Zvláštní problém při zálzoe zdroje {source.id}", ex);

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
                logInfo("Zipuji zálohu");
                zip_path = Path.Combine(temp_instance_dir, "temp.zip");
                ZipFile.CreateFromDirectory(source_dir, zip_path);
                return true;
            }
            catch (Exception ex)
            {
                error("Nepodařilo se zazipovat zdroj.", ex);
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

                logInfo($"Nahrávám {src} přes sftp do {remote_path}");
                Progress?.Update(BackupState.SftpUpload, 0);


                //průběžně budeme posílat GUI info o tom, kolik bytů již bylo staženo.
                //nechceme to ale posílat moc často, takže to ohlídáme pomocí Stopwatch.
                Stopwatch progress_interval_limiter = new Stopwatch();
                progress_interval_limiter.Start();

                if (Rule.Zip)
                {
                    float total_length = (float)new FileInfo(src).Length;

                    sftp.UploadFile(src, remote_path, ul =>
                    {
                        if (progress_interval_limiter.ElapsedMilliseconds > Const.UPDATE_GUI_SFTP_UPLOAD_MIN_MS_INTERVAL)
                        {
                            float part = ul / total_length;
                            Progress?.Update(BackupState.SftpUpload, part, $"{Math.Ceiling(part * 100)} %");
                            progress_interval_limiter.Restart();
                        }
                    });
                    
                }
                else
                {
                    float total_length = (float)FileUtils.GetDirSize(src);

                    sftp.UploadDirectory(src, remote_path, FolderUploadBehavior.ReplaceWhole, null, ul =>
                    {
                        if (progress_interval_limiter.ElapsedMilliseconds > Const.UPDATE_GUI_SFTP_UPLOAD_MIN_MS_INTERVAL)
                        {
                            float part = ul / total_length;
                            Progress?.Update(BackupState.SftpUpload, part, $"{Math.Ceiling(part * 100)} %");
                            progress_interval_limiter.Restart();
                        }
                    });
                }

                progress_interval_limiter.Stop();

                B_Obj.AvailableRemotely = true;
                B_Obj.RemotePath = remote_path;
                return true;
            }
            catch(Exception ex)
            {
                B_Obj.Success = false;
                B_Obj.AvailableRemotely = false;
                error($"Problém s nahráváním souboru přes SFTP na server", ex);
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
            string this_bk_path = Path.GetFullPath(Path.Combine(rule_folder, fname));

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
                error($"Nepodařilo se uložit zálohu lokálně.", ex);
                return false;
            }
        }

        private async Task finishBackup()
        {
            await saveInfo();
            updateLastExecutionInfo();

            //odeslat mail o záloze
            Manager.Get<SmbMailer>()?.ReportBackupAsync(B_Obj);

            //odpojení od serverů, vyčištění stínových kopií, odstranění dočasných souborů
            disconnectSftp();
            disconnectSql();
            cleanUpVss();
            deleteTempFolder();

            //logování
            if (B_Obj.Success)
                logInfo($"Pravidlo {Rule.Name} úspěšně uplatněno.");
            else
                SmbLog.Warn($"Během vyhodnocování pravidla {Rule.Name} došlo k chybám.", null, LogCategory.BackupTask);

            //update progresu
            Progress?.Update(BackupState.Done, 0);
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
                logError("Problém při ukládání info o proběhnuté záloze", ex);
                return false;
            }
        }

        private void updateLastExecutionInfo()
        {
            try
            {
                Rule.LastExecution = DateTime.Now;
                Manager.Get<BackupRuleLoader>().Update(Rule);
            }
            catch(Exception ex)
            {
                logError("Nepodařilo se nastavit poslední datum vyhodnocení pravidla.", ex);
            }
        }

        private bool disconnectSftp()
        {
            logInfo("Odpojuji se od sftp");
            if (sftp != null && sftp.IsConnected)
            {
                try
                {
                    sftp.Disconnect();
                    return true;
                }
                catch (Exception ex)
                {
                    logError("Problém při odpojování od sftp", ex);
                    return false;
                }
            }

            return true;
        }

        private bool disconnectSql()
        {
            logInfo("Odpojuji se od sql");
            if (sql != null && sql.connection.State == System.Data.ConnectionState.Open)
            {
                try
                {
                    sql.Close();
                    return true;
                }
                catch (Exception ex)
                {
                    logError("Prolbém při odpojování od SQL serveru", ex);
                    return false;
                }

            }

            return false;
        }

        private void cleanUpVss()
        {
            logInfo("Odstraňuji shadow copy");
            foreach(var pair in shadowCopies)
            {
                try
                {
                    pair.Value.Dispose();
                }
                catch (Exception ex)
                {
                    logError("Problém při odstraňování shadow copy", ex);
                }
            }
        }

        private bool deleteTempFolder()
        {
            if (temp_instance_dir != null)
            {
                try
                {
                    logInfo("Odstraňuji dočasnou složku");
                    return FileUtils.DeleteFolder(temp_instance_dir);
                }
                catch (Exception ex)
                {
                    logError("Problém při odstraňování dočasné složky", ex);
                    return false;
                }
            }

            return true;
        }
    }
}
