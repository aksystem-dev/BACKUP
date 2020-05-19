using SmartModulBackupClasses;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace smart_modul_BACKUP_service
{
    /// <summary>
    /// Vyhodnocuje pravidla a provádí podle nich zálohy.
    /// </summary>
    public class Backuper
    {
        public string TempDir;
        //public string SftpBackupDir;
        //public string LocalBackupDir;
        //public bool UseShadowCopy = false;

        public Backuper()
        {
        }

        /// <summary>
        /// Odstraní všechny prošlé zálohy daného pravidla.
        /// </summary>
        /// <param name="rule"></param>
        /// <param name="infos"></param>
        public void BackupCleanUpByRule(SftpUploader Sftp, BackupRule rule, List<BackupError> errors = null, bool saveAtEnd = true)
        {
            var infos = Utils.SavedBackups.GetInfos().Where(f => f.RefRule == rule.LocalID);

            //odstranění přebytečných lokálních záloh
            var local = infos.Where(f => f.AvailableLocally).ToArray();
            var overflow = local.Length - rule.LocalBackups.MaxBackups;
            if (overflow > 0)
            {
                local = local.OrderBy(f => f.EndDateTime).ToArray();
                for (int i = 0; i < overflow; i++)
                {
                    var current = local[i];
                    current.AvailableLocally = false;

                    try
                    {
                        File.Delete(current.LocalPath);
                        Logger.Log($"Odstraněna lokální zálohu pravidla {current.RefRuleName} umístěnou na adrese {current.LocalPath}");
                    }
                    catch (Exception e)
                    {
                        string msg = $"Problém s odstraňováním staré lokální zálohy pravidla {current.RefRuleName} umístěné na adrese {current.LocalPath}\n\n{e.Message}";
                        Logger.Error(msg);
                        errors?.Add(new BackupError(msg, BackupErrorType.IOError));
                    }
                }
            }

            //pokud se neumíme připojit přes sftp, nemá cenu pokračovat
            if (Sftp == null)
                return;

            //odstranění přebytečných remote záloh
            var remote = infos.Where(f => f.AvailableRemotely).ToArray();
            overflow = remote.Length - rule.RemoteBackups.MaxBackups;
            if (overflow > 0)
            {
                remote = remote.OrderBy(f => f.EndDateTime).ToArray();
                for (int i = 0; i < overflow; i++)
                {
                    var current = remote[i];
                    current.AvailableRemotely = false;

                    try
                    {
                        Sftp.Delete(current.RemotePath);
                        Logger.Log($"Odstraněna remote záloha pravidla {current.RefRuleName} umístěná na adrese {current.RemotePath}");
                    }
                    catch (Exception e)
                    {
                        string msg = $"Problém s odstraňováním staré remote zálohy pravidla {current.RefRuleName} umístěné na adrese {current.RemotePath}\n\n{e.Message}";
                        Logger.Error(msg);
                        errors?.Add(new BackupError(msg, BackupErrorType.SftpError));
                    }
                }
            }

            if (saveAtEnd)
            {
                Utils.SavedBackups.RemoveInfos(f => !f.AvailableLocally && !f.AvailableRemotely);
                Utils.SavedBackups.SaveInfos();
            }
        }

        private delegate bool ExistsChecker(string path);

        private string _backupSourceZip(BackupSource source, SqlBackuper sqlBackuper, Dictionary<string, VssBackuper> shadowCopies, string dir, string filename)
        {
            //toto bude cesta k dočasnému uložení zdroje.
            string temp_source_path = Path.Combine(dir, filename);

            if (source.type == BackupSourceType.Database)
            {
                //zde porychtujeme databáze

                //pokud nemáme instanci SqlBackuperu (např. se připojení nezdařilo), kašlem na to
                if (sqlBackuper == null)
                    return null;

                //vytvořit složku pro zip
                Directory.CreateDirectory(temp_source_path);

                //zálohovat databázi
                _backupDatabase(sqlBackuper, source.path, Path.Combine(temp_source_path, filename + ".bak"));

                //vytvořit zip soubor
                string zip_path = temp_source_path + ".zip";
                ZipFile.CreateFromDirectory(temp_source_path, zip_path);

                //odstranit původní složku, ponechat pouze zip
                Directory.Delete(temp_source_path, true);

                return zip_path;
            }
            else if (source.type == BackupSourceType.Directory)
            {
                //zde porychtujeme složky

                string root = Path.GetPathRoot(source.path);
                string zip_path = temp_source_path + ".zip";

               //vytvořit zip archiv
                ZipFile.CreateFromDirectory(
                    //pokud máme pro tento volume Shadow Copy, budeme zipovat Shadow Copy; jinak normálně ten soubor
                    shadowCopies.ContainsKey(root) ? shadowCopies[root].GetShadowPath(source.path) : source.path,
                    zip_path);
                return zip_path;
            }
            else
                throw new ArgumentException($"Neznám typ zdroje {source.type}.");
        }

        /// <summary>
        /// Postará se o zálohu databáze na dané místo.
        /// </summary>
        /// <param name="database"></param>
        /// <param name="targetPath"></param>
        private void _backupDatabase(SqlBackuper backuper, string database, string targetPath)
        {
            backuper.Backup(database, targetPath);
        }

        /// <summary>
        /// Vyhodnotí dané pravidlo a všechny zdroje uloží do jednoho zipu.
        /// </summary>
        /// <param name="rule">Pravidlo k vyhodnocení.</param>
        /// <returns>Zdali bylo vyhodnocování úspěšné.</returns>
        public Backup ExecuteRuleSingleZip(BackupRule rule, bool cleanupAfterwards)
        {
            #region INIT

            Utils.GUIS.BackupStarted(rule.Name);
            Logger.Log($"Pravidlo {rule.Name} (id {rule.LocalID}) spuštěno");

            //objekt s informacemi o této záloze, který posléze uložíme do souboru
            var B = new Backup()
            {
                RefRule = rule.LocalID,
                RefRuleName = rule.Name,
                ID = Utils.SavedBackups.ReserveId(),
                Errors = new List<BackupError>(),
                Sources = new List<SavedSource>(),
                Success = true,
                StartDateTime = DateTime.Now,
                ComputerId = SMB_Utils.GetComputerId()
            };

            Utils.SavedBackups.AddInfo(B);

            //ujistit se, že TempDir existuje
            Directory.CreateDirectory(TempDir);

            //tato složka by měla být využívána pouze tímto konkrétním vyhodnocením metody.
            //na konci této metody je třeba ji odstranit
            string temp_instance_dir = Path.Combine(TempDir, "backup" + Directory.GetDirectories(TempDir).Length.ToString());

            //Ujistit se, že existuje složka, kam budeme ukládat dočasné soubory
            Directory.CreateDirectory(temp_instance_dir);

            #endregion

            #region CONNECT SFTP
            //pokud toto pravidlo dělá zálohy přes Sftp (nebo v minulosti dělalo, čili existuje sftp záloha), připojit se přes Sftp
            SftpUploader Sftp = null;
            if ((rule.RemoteBackups.enabled && rule.RemoteBackups.MaxBackups > 0) ||
                Utils.SavedBackups.GetInfos().Any(f=>f.RefRule == rule.LocalID && f.AvailableRemotely))
            {
                Logger.Log("Připojuji se k SFTP");
                try
                {
                    Sftp = Utils.SftpFactory.GetInstance();
                    Sftp.Connect();
                }
                catch (Exception e)
                {
                    Logger.Error($"Nepodařilo se připojit k SFTP (chyba {e.GetType().Name})\n\n{e.Message}");
                    Utils.GUIS.ShowError("Službě smart modul BACKUP se nepodařilo připojit ke vzdálenému úložišti.");

                    B.Errors.Add(new BackupError(
                        $"Nepodařilo se připojit k SFTP (chyba {e.GetType().Name})\n\n{e.Message}",
                        BackupErrorType.SftpError
                        ));

                    B.Success = false;
                    Sftp = null;
                }
            }

            #endregion

            #region CONNECT SQL

            //pokud toto pravidlo obsahuje databáze, připojit se přes SqlBackuper
            SqlBackuper SqlBackuper = null;
            if (rule.Sources.Databases.Any())
            {
                Logger.Log("Připojuji se přes SQL");
                try
                {
                    SqlBackuper = Utils.SqlFactory.GetInstance();
                    SqlBackuper.Open();
                    Logger.Log("Úspěšně připojeno k SQL serveru");
                }
                catch (Exception e)
                {
                    Logger.Error($"Nepodařilo se připojit přes SQL (chyba {e.GetType().Name} \n\n{e.Message}");
                    Utils.GUIS.ShowError("Službě smart modul BACKUP se nepodařilo připojit k SQL serveru.");

                    B.Errors.Add(new BackupError(
                        $"Nepodařilo se připojit přes SQL (chyba {e.GetType().Name} \n\n{e.Message}",
                        BackupErrorType.SqlError
                        ));

                    B.Success = false;
                    SqlBackuper = null;
                }
            }

            #endregion

            #region MAKE SHADOW COPIES

            //pokud je povoleno shadow copy, vytvoříme pro každý kořen objekt VssBackuper, který zazálohuje daný volume
            Dictionary<string, VssBackuper> shadowCopies = new Dictionary<string, VssBackuper>();

            if (Utils.Config.UseShadowCopy)
            {
                Logger.Log("Vytvářím Shadow Copy");

                foreach (var src in rule.Sources.Directories)
                {
                    string root = Path.GetPathRoot(src.path);
                    if (!shadowCopies.ContainsKey(root)) //pokud jsme pro tento svazek ještě Shadow Copy nevytvořili
                    {
                        try
                        {
                            Logger.Log($"Chystám se na Shadow Copy svazku {root}");

                            var shadowCopy = new VssBackuper();
                            shadowCopy.DoBackup(root, B.Errors);
                            shadowCopies.Add(root, shadowCopy);

                            Logger.Log($"Shadow Copy svazku {root} vytvořena");
                        }
                        catch (Exception ex)
                        {
                            Logger.Log(ex.Message);
                        }
                    }
                }

                Logger.Log("Shadow Copy vytvořeny");
            }

            #endregion

            string dir_to_zip = Path.Combine(temp_instance_dir, "dir");
            Directory.CreateDirectory(dir_to_zip);

            //Projdeme všechny povolené zdroje pravidla
            foreach (var source in rule.Sources.All.Where(f => f.enabled))
            {
                #region CREATE TEMP ZIP FILE BACKUP

                //string temp_filename = source.id;// + "_" + DateTime.Now.ToString("dd-MM-yyyy");

                //nejprve vytvoříme jednu zálohu daného zdroje
                //string temp_file_path;

                //try
                //{
                //    Logger.Log($"Vytvářím zálohu zdroje {source.id}");

                //    //vytvořit zálohu zdroje
                //    temp_file_path = _backupSource(source, SqlBackuper, shadowCopies, dir_to_zip);

                //    //pokud se záloha nevytvořila, temp_zip_path bude null, a potom se chceme na tento zdroj vykašlat a hupnout dál
                //    if (temp_file_path == null)
                //        continue;

                //    //přidat info o zdroji
                //    B.Sources.Add(new SavedSource()
                //    {
                //        filename = Path.GetFileName(temp_file_path),
                //        sourcepath = source.path,
                //        type = source.type
                //    });

                //}
                //catch (Exception e)
                //{
                //    //pokud se nepodaří vytvořit zip, vyplivnem chybu a dáme se na další zdroj
                //    Logger.Error($"Nepodařilo se zazálohovat zdroj {source.path} pravidla {rule.Name}. Došlo k chybě {e.GetType().Name}.\n\n{e.Message}");

                //    B.Errors.Add(new BackupError(
                //        $"Nepodařilo se zazálohovat zdroj {source.path} pravidla {rule.Name}. Došlo k chybě {e.GetType().Name}.\n\n{e.Message}",
                //        BackupErrorType.IOError, source.id
                //        ));

                //    B.Success = false;
                //    continue;
                //}

                Logger.Log($"Vytvářím zálohu zdroje {source.id}");

                string temp_fpath = null;
                string error = null;
                string error_detail = null;
                var src_success = BackupSuccessLevel.SomeErrors;

                try
                {
                    //tato metoda se pokusí provést zálohu databáze, složky nebo souboru, a vrátí případné chyby
                    temp_fpath = _backupSource(source, SqlBackuper, shadowCopies, dir_to_zip, out error, out error_detail, out src_success);
                }
                catch (Exception e)
                {
                    //pokud dojde k výjimce, zpráva z výjimky se uloží jako chyba
                    error = "Došlo k neošetřené výjimce";
                    error_detail = $"Výjimka {e.GetType().Name}:\n{e.Message}";
                    src_success = BackupSuccessLevel.TotalFailure;
                }
                finally
                {
                    //vytvořit objekt informací o záloze zdroje
                    var saved_source = new SavedSource()
                    {
                        Error = error,
                        ErrorDetail = error_detail,
                        Success = src_success,
                        filename = temp_fpath != null ? Path.GetFileName(temp_fpath) : null,
                        sourcepath = source.path,
                        type = source.type
                    };

                    //uložit info o zdroji zálohy do objektu zálohy
                    B.Sources.Add(saved_source);

                    Logger.Log($"Záloha zdroje {source.id} hotova; výsledný objekt:\n{saved_source.PropertiesString()}");
                }

                #endregion

            }

            #region ZIP FOLDER

            //kam uložíme dočasný zip
            string temp_zip_path = Path.Combine(temp_instance_dir, "temp.zip");

            try
            {
                ZipFile.CreateFromDirectory(dir_to_zip, temp_zip_path);
                B.Size = new FileInfo(temp_zip_path).Length;
            }
            catch (Exception e)
            {
                string errmsg = $"Nepodařilo se zazipovati zálohu pravidla {rule.Name}\n\n{e.GetType().Name}\n\n{e.Message}";
                Logger.Error(errmsg);

                B.Errors.Add(new BackupError(errmsg, BackupErrorType.IOError));

                B.Success = false;

                //pokud se zazipování nepodařilo, tak víme, že nemá cenu se soubor snažit někam kopírovat nebo uploadovat,
                //takže tyhle věci přeskočíme
                goto after_backups;
            }

            #endregion

            //název zip souboru zálohy
            string zip_fname = rule.Name + "_" + B.ID.ToString() + "_" + DateTime.Now.ToString("dd-MM-yyyy") + ".zip";

            #region UPLOAD TO SFTP

            if (rule.RemoteBackups.enabled && Sftp != null)
            {
                //zde se už jedná o konkrétní zálohu.

                //vytvořit cestu k cíli na sftp serveru
                string remote_path = Path.Combine(Utils.Config.RemoteBackupDirectory, rule.Name, zip_fname);

                try
                {
                    //nahrát soubor na server
                    Sftp.Upload(temp_zip_path, remote_path);

                    B.AvailableRemotely = true;
                    B.RemotePath = remote_path;
                }
                catch (Exception e)
                {
                    B.Success = false;
                    Logger.Error($"Problém s nahráváním zálohy přes SFTP na server. (chyba {e.GetType().Name}\n\n {e.Message}");

                    B.Errors.Add(new BackupError(
                        $"Problém s nahráváním souboru přes SFTP na server. (chyba {e.GetType().Name}\n\n {e.Message}",
                        BackupErrorType.SftpError
                        ));

                }
            }

            #endregion

            #region SAVE LOCALLY

            if (rule.LocalBackups.enabled)
            {
                //budeme vytvářet lokální zálohy a ty zálohy budou lokální, tedy uložené lokálně, aby bylo jasno.

                string zip_folder = Path.Combine(Utils.Config.LocalBackupDirectory, rule.Name);
                Directory.CreateDirectory(zip_folder);

                //zjistit id zálohy a cestu, kam chceme zálohu uložit
                string zip_path = Path.Combine(zip_folder, zip_fname);

                try
                {
                    //přesunout dočasný zip na místo, kde to již nebude dočasné
                    File.Move(temp_zip_path, zip_path);

                    B.AvailableLocally = true;
                    B.LocalPath = zip_path;
                }
                catch (Exception e)
                {
                    string errmsg = $"Chyba při zálohování dle pravidla {rule.Name}: asi se nepodařilo zkopírovat {temp_zip_path} do {zip_path}\n\n{e.GetType().Name}\n\n{e.Message}";
                    Logger.Error(errmsg);

                    B.Errors.Add(new BackupError(errmsg, BackupErrorType.IOError));

                    B.Success = false;
                }
            }

        #endregion


        after_backups:

            #region SAVE RULE EXECUTION INFO

            B.EndDateTime = DateTime.Now;

            try
            {
                Utils.SavedBackups.RemoveInfos(f => !f.AvailableLocally && !f.AvailableRemotely);
                Utils.SavedBackups.SaveInfos();
                Logger.Log("Info o záloze uloženo");
            }
            catch (Exception e)
            {
                string errmsg = $"Nepodařilo se uložit informace o záloze {rule.Name}\n\n{e.GetType().Name}\n\n{e.Message}";
                Logger.Error(errmsg);
                B.Errors.Add(new BackupError(errmsg, BackupErrorType.IOError));
                B.Success = false;
            }

            #endregion

            #region REMOVE OLD BACKUPS

            //pokud cleanupafterwards == true, musíme odstranit staré zálohy
            if (cleanupAfterwards)
                try
                {
                    BackupCleanUpByRule(Sftp, rule, B.Errors, false);
                }
                catch (Exception e)
                {
                    string msg = $"Problém s odstraňováním starých záloh ({rule.Name})\n\n{e.Message}";
                    Logger.Error(msg);
                    B.Errors.Add(new BackupError(msg, BackupErrorType.DefaultError));
                }

            #endregion

            #region DISCONNECT SFTP

            //Pokud jsme připojeni přes SFTP, odpojíme se
            if (Sftp != null)
            {
                try
                {
                    Sftp.Disconnect();
                    Sftp.client.Dispose();
                }
                catch (Exception e)
                {
                    Logger.Error($"Problém s odpojování od SFTP serveru (chyba {e.GetType().Name})\n\n{e.Message}");
                    B.Success = false;
                }
            }

            #endregion

            #region DISCONNECT SQL

            //Pokud jsme připojeni přes SQL, odpojíme se
            if (SqlBackuper != null)
            {
                try
                {
                    SqlBackuper.Close();
                    SqlBackuper.connection.Dispose();
                }
                catch (Exception e)
                {
                    Logger.Error($"Problém s odpojování od SFTP serveru (chyba {e.GetType().Name})\n\n{e.Message}");
                    B.Success = false;
                }
            }

            #endregion

            #region CLEAN UP SHADOW COPIES

            //Také musíme porychtovat VssBackupery

            foreach (var pair in shadowCopies)
                try
                {
                    pair.Value.Dispose();
                }
                catch (Exception ex)
                {
                    Logger.Error($"Problém s odstraňováním Shadow Copy: {ex.Message}");
                }

            #endregion

            #region DELETE TEMP FOLDER

            //nakonec odstranit dočasnou složku
            try
            {
                Directory.Delete(temp_instance_dir, true);
            }
            catch (Exception ex)
            {
                Logger.Error($"Problém s odstraňováním dočasné složky ({ex.GetType().Name}):\n\n{ex.Message}");
            }

            #endregion

            #region LOG SUCCESS

            if (B.Success)
                Logger.Success($"Pravidlo {rule.Name} úspěšně uplatněno.");
            else
                Logger.Failure($"Pravidlo {rule.Name} uplatněno, ale došlo k chybám.");

            //informovat gui o záloze
            Utils.GUIS.BackupEnded(rule.Name, B.Success);

            #endregion

            return B;
        }

        /// <summary>
        /// provede zálohu daného zdroje.
        /// </summary>
        /// <param name="source">daný zdroj</param>
        /// <param name="sqlBackuper">SqlBackuper, který se použije</param>
        /// <param name="shadowCopies">Seznam ShadowCopies k použití</param>
        /// <param name="dir">Cílový adresář</param>
        /// <returns></returns>
        private string _backupSource(BackupSource source, SqlBackuper sqlBackuper, Dictionary<string, VssBackuper> shadowCopies, string dir, out string error, out string error_detail, out BackupSuccessLevel success)
        {
            Logger.Log($"_backupSourceFile called for {source.type} {source.id}");
            Directory.CreateDirectory(dir);

            if (source.type == BackupSourceType.Database)
            {
                //zde porychtujeme databáze

                //pokud nemáme instanci SqlBackuperu (např. se připojení nezdařilo), kašlem na to
                if (sqlBackuper == null)
                {
                    error = "Nelze zálohovat databázi, neboť se nepodařilo připojit k SQL serveru.";
                    error_detail = null;
                    success = BackupSuccessLevel.TotalFailure;
                    return null;
                }

                //sestavit cestu k záloze
                string bak_path = Path.Combine(dir, source.id + ".bak");

                //zálohovat databázi
                _backupDatabase(sqlBackuper, source.path, bak_path);

                error = null;
                error_detail = null;
                success = BackupSuccessLevel.EverythingWorked;
                return bak_path;
            }
            else if (source.type == BackupSourceType.Directory)
            {
                //zde porychtujeme složky

                string root = Path.GetPathRoot(source.path);

                //sestavit cestu k záloze
                string dir_path = Path.Combine(dir, source.id);

                List<string> failed_paths = new List<string>();

                bool copied_all = FolderCopier.CopyFolderContents(
                    //pokud máme pro tento volume Shadow Copy, budeme zipovat Shadow Copy; jinak normálně ten soubor
                    shadowCopies.ContainsKey(root) ? shadowCopies[root].GetShadowPath(source.path) : source.path,
                    dir_path, failed_paths);

                if (copied_all)
                {
                    success = BackupSuccessLevel.EverythingWorked;
                    error = null;
                    error_detail = null;
                }
                else
                {
                    success = BackupSuccessLevel.SomeErrors;
                    error = "Nepodařilo se zkopírovat některé soubory.";
                    error_detail = String.Join("\n", failed_paths);
                }

                return dir_path;
            }
            else if (source.type == BackupSourceType.File)
            {
                //zde porychtujeme složky

                string root = Path.GetPathRoot(source.path);

                //sestavit cestu k záloze
                string file_path = Path.Combine(dir, source.id + Path.GetExtension(source.path));

                try
                {
                    File.Copy(
                        //pokud máme pro tento volume Shadow Copy, budeme zipovat Shadow Copy; jinak normálně ten soubor
                        shadowCopies.ContainsKey(root) ? shadowCopies[root].GetShadowPath(source.path) : source.path,
                        file_path, true);

                    success = BackupSuccessLevel.EverythingWorked;
                    error = null;
                    error_detail = null;
                    return file_path;
                }
                catch (Exception e)
                {
                    success = BackupSuccessLevel.TotalFailure;
                    error = e.Message;
                    error_detail = $"Výjimka {e.GetType().Name}:\n{e.Message}";
                    return null;
                }
            }
            else
            {
                success = BackupSuccessLevel.TotalFailure;
                error = $"Neznám typ zdroje {source.type}";
                error_detail = null;
                return null;
            }
                //throw new ArgumentException($"Neznám typ zdroje {source.type}.");
        }

    }
}
