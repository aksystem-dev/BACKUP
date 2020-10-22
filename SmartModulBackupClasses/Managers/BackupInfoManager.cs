using Renci.SshNet;
using SmartModulBackupClasses.WebApi;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SmartModulBackupClasses.Managers
{
    public class BackupInfoLoadOptions
    {
        /// <summary>
        /// zdali se mají stahovat informace ze SFTP serveru
        /// </summary>
        public bool DownloadSFTP { get; set; }

        /// <summary>
        /// Zdali se mají nahrávat informace na SFTP server
        /// </summary>
        public bool UploadSFTP { get; set; }

        /// <summary>
        /// zdali se mají stahovat informace z webové aplikace
        /// </summary>
        public bool DownloadApi { get; set; }

        /// <summary>
        /// Zdali se mají nahrávat informace na webovou aplikaci
        /// </summary>
        public bool UploadApi { get; set; }

        /// <summary>
        /// pokud DownloadApi == false, vrátí false. Jinak vrátí, zdali jsme připojeni k api.
        /// </summary>
        public bool ShouldDownloadApi
        {
            get
            {
                if (!DownloadApi)
                    return false;

                var aman = Manager.Get<AccountManager>();
                return aman.Connected;
            }
        }

        /// <summary>
        /// pokud DownloadApi == false, vrátí false. Jinak vrátí, zdali jsme připojeni k api.
        /// </summary>
        public bool ShouldUploadApi
        {
            get
            {
                if (!UploadApi)
                    return false;

                var aman = Manager.Get<AccountManager>();
                return aman.Connected;
            }
        }

        /// <summary>
        /// funkce, podle níž se bude filtrovat, které zálohy se načtou
        /// </summary>
        public Func<Backup, bool> BackupFilter { get; set; }

        /// <summary>
        /// funkce, podle níž se bude filtrovat, z kterých klientů se budou
        /// stahovat zálohy přes SFTP (pouze, pokud UseSftp == true)
        /// </summary>
        public Func<PC_Info, bool> SftpClientFilter { get; set; }

        /// <summary>
        ///  pokud true, metoda se postará o to, aby na žádném ze zdrojů 
        ///     (lokální, pokud UseApi => webové api, pokud UseSftp => sftp server) 
        ///     nechybělo žádné info, které není na jiném zdroji
        /// </summary>
        public bool Sync { get; set; }

        /// <summary>
        /// zkopíruje tuto instanci
        /// </summary>
        /// <returns></returns>
        public BackupInfoLoadOptions Copy()
        {
            return new BackupInfoLoadOptions()
            {
                BackupFilter = BackupFilter,
                SftpClientFilter = SftpClientFilter,
                Sync = Sync,
                DownloadApi = DownloadApi,
                DownloadSFTP = DownloadSFTP,
                UploadApi = UploadApi,
                UploadSFTP = UploadApi        
            };
        }

        /// <summary>
        /// vrátí upravenou kopii této instance
        /// </summary>
        /// <param name="change"></param>
        /// <returns></returns>
        public BackupInfoLoadOptions With(Action<BackupInfoLoadOptions> change)
        {
            var instance = this.Copy();
            change(instance);
            return instance;
        }
    }

    /// <summary>
    /// Spravuje informace o proběhnutých zálohách
    /// </summary>
    public class BackupInfoManager : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Při dělání operací na tomto objektu se používá semafor. Toto číslo udává v milisekundách, jak dlouho
        /// budeme maximálně na semaforu čekat
        /// </summary>
        public int Patience { get; set; } = 2000;

        /// <summary>
        /// seznam načtených záloh
        /// </summary>
        private List<Backup> _backups = new List<Backup>();

        /// <summary>
        /// nastavení, které se použije při stahování informací, pakliže nejsou jiné informace
        /// dány při volání fce; toto nastavení se také používá při operacích jako Add,
        /// Delete, ...
        /// </summary>
        public BackupInfoLoadOptions DefaultOptions = new BackupInfoLoadOptions()
        {
            UploadApi = true,
            UploadSFTP = true,
            DownloadApi = false,
            DownloadSFTP = false,
            Sync = true,
            BackupFilter = new Func<Backup, bool>(bk => true),
            SftpClientFilter = new Func<PC_Info, bool>(pc => pc.IsThis)
        };

        /// <summary>
        /// Defaultní filtr pro filtrování načtených záloh. Defaultně projdou všechny zálohy.
        /// </summary>
        public Func<Backup, bool> DefaultFilter { get; set; } = new Func<Backup, bool>(bk => true);

        /// <summary>
        /// Seznam všech načtených záloh.
        /// </summary>
        public Backup[] Backups
        {
            get => _backups.ToArray();
        }

        /// <summary>
        /// Seznam načtených záloh, které byly vytvořeny na tomto počítači.
        /// </summary>
        public IEnumerable<Backup> LocalBackups
        {
            get
            {
                var pc_id = SMB_Utils.GetComputerId();
                return _backups.Where(f => f.ComputerId == pc_id);
            }
        }

        SmbApiClient client => Manager.Get<AccountManager>()?.Api;
        readonly ConfigManager config;
        readonly AccountManager plans;

        public BackupInfoManager()
        {
            config = Manager.Get<ConfigManager>();
            plans = Manager.Get<AccountManager>();
        }


        /// <summary>
        /// Pokud se tato třída využívá ve WPF, sem dejte něco jako Application.Current.Dispatcher.InvokeAsync;
        /// jinak to bude skuhrat, že měníme GUI z jiného vlákna
        /// </summary>
        public Action<Action> PropertyChangedDispatchHandler;

        void arrayChanged()
        {
            var dgate = PropertyChangedDispatchHandler ?? new Action<Action>(a => a());
            dgate.Invoke(() =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Backups)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LocalBackups)));
            });
        }

        /// <summary>
        /// Aby nedocházelo k konfliktům, měla by vždy probíhat jen jedna operace najednou
        /// (nejen v tomto programu, ale ve všech programech využívajících tuto třídu -
        /// čili jak Windows Služba tak GUI)
        /// </summary>
        private Semaphore semaphore = new Semaphore(1, 1, "SMB_BackupInfoManager_Semaphore");

        public async Task LoadAsync()
        {
            await LoadAsync(DefaultOptions);
        }

        public async Task LoadAsync(BackupInfoLoadOptions options)
        {
            bool entered = semaphore.WaitOne(Patience);
            try
            {
                await _loadAsync(options);
            }
            finally
            {
                if (entered)
                    semaphore.Release();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sync">pokud true, budou se doplňovat chybějící informace na všechny zvolené strany (lokálně / api / sftp)</param>
        /// <returns></returns>
        private async Task _loadAsync(BackupInfoLoadOptions options)
        {
            var newBackups = new List<Backup>();

            IEnumerable<Backup> apiResults = Enumerable.Empty<Backup>(); //zde budou všechny zálohy z aktivovaného plánu (čili mohou být i z jiných počítačů)
            IEnumerable<Backup> sftpResults = Enumerable.Empty<Backup>();
            IEnumerable<Backup> localResults = Enumerable.Empty<Backup>();

            List<Task> listTasks = new List<Task>();

            //používáme-li api, stáhnem z něj informace o zálohách
            if (options.ShouldDownloadApi)
                listTasks.Add(Task.Run(async () => apiResults = await listBksApi()));

            //také je možné, že jsou informace uložené na sftp;
            SftpUploader sftp = options.DownloadSFTP ? Manager.Get<SftpUploader>() : null;
            Dictionary<string, string> remoteBkinfoPaths = new Dictionary<string, string>();
            if (sftp != null)
            {
                listTasks.Add(Task.Run(async () =>
                {
                    if ((await sftp?.TryConnectAsync(2000)) == true)
                        sftpResults = await listBksSftp(sftp, options.SftpClientFilter, remoteBkinfoPaths);
                }));
            }


            //a konečně jsou také uložené lokálně
            Dictionary<string, string> localBkinfoPaths = new Dictionary<string, string>();
            listTasks.Add(Task.Run(async () => localResults = await listBksLocal(localBkinfoPaths)));

            //posečkáme, až všechny tři úlohy sklidí ovoce své píle
            await Task.WhenAll(listTasks);

            //teď přidáme do seznamu všechny získané informace. Budeme je přidávat postupně tak, aby nikdy nebylo
            //více záloh se stejným LocalID ze stejného počátače. Nejprve přidáme zálohy z api. Api vrací zálohy
            //nejen z volajícího počítače, ale ze všech počítačů napojených na jeho plán. Potom do toho zamícháme
            //i informace z lokálních souborů a souborů na sftp; ty jsou zaručeně jen z tohoto počítače.

            var added = new HashSet<string>();

            //funkce, kterou se budou filtrovat přidané zálohy. 
            Func<Backup, bool> canAdd = new Func<Backup, bool>(bk =>
            {
                var hash = bk.UniqueHash;

                if (added.Contains(hash))
                    return false;

                added.Add(hash);
                return true;
            });

            newBackups.AddRange(apiResults);
            newBackups.AddRange(sftpResults.Where(canAdd));
            newBackups.AddRange(localResults.Where(canAdd));

            //nastavit cesty, odkud jsme zálohy vzali
            foreach (var bk in newBackups)
            {
                var hash = bk.UniqueHash;

                if (remoteBkinfoPaths.ContainsKey(hash))
                    bk._savedBkinfoRemotePath = remoteBkinfoPaths[hash];
                if (localBkinfoPaths.ContainsKey(hash))
                    bk._savedBkinfoLocalPath = localBkinfoPaths[hash];
            }

            //nastavit pole backups podle fitrovaného newBackups
            _backups.UpdateCollectionByCompare(
                newBackups.Where(options.BackupFilter),
                (b1, b2) =>
                {
                    if (!b1.MadeOnThisComputer)
                        return false;
                    return b1.LocalID == b2.LocalID;
                });

            List<Task> sftpTasks = new List<Task>();
            List<Task> apiTasks = new List<Task>();
            List<Task> localTasks = new List<Task>();

            if (options.Sync)
            { 
                //máme tři zdroje informací o zálohách - api, sftp a lokální soubory. Chceme, aby na všech zdrojích
                //byly všechny zálohy. o to se postaráme zde
                foreach (var bk in Backups) 
                {
                    if (bk.MadeOnThisComputer) //na SFTP a API nahráváme jen zálohy, které jsme si sami vytvořili
                    {
                        //pokud stahujen zálohu i s Api
                        //a zároveň narazíme na zálohu, která byla vytvořena k aktuálnímu plánu, ale webové api o ni neví,
                        //nahrajeme jí tam
                        if (options.ShouldDownloadApi && !apiResults.Any(f => f.LocalID == bk.LocalID) && client != null && bk.UploadedToCurrentPlan)
                            apiTasks.Add(saveBkApi(bk));

                        //pokud stahujem zálohu i ze SFTP
                        //a zároveň narazíme na zálohu, která byla nahrána na aktuální SFTP server ale nebylo o ní nahráno info,
                        //nahrajeme jí tam
                        if (options.DownloadSFTP && !sftpResults.Any(f => f.LocalID == bk.LocalID) && sftp?.IsConnected == true)
                            sftpTasks.Add(saveBkSftp(bk, sftp));
                    }

                    //pokud info o záloze není uloženo lokálně, uložíme ho
                    if (!bk.MadeOnThisComputer || !localResults.Any(f => f.LocalID == bk.LocalID))
                        localTasks.Add(saveBkLocally(bk));
                }
            }

            //až budou všechny sftp tasky hotové, chceme se od sftp odpojit
            await Task.WhenAll(sftpTasks).ContinueWith(task => sftp?.Disconnect(false));
            await Task.WhenAll(apiTasks);
            await Task.WhenAll(localTasks);

            SmbLog.Info("Zz");

            //kváknem že se změnila data, aby na to mohlo reagovat třeba UI
            arrayChanged();
        }

        /// <summary>
        /// Přidá info o záloze: uloží ho lokálně, popř. na SFTP, popř. na WEB
        /// </summary>
        /// <param name="bk"></param>
        public async Task AddAsync(Backup bk)
        {
            bool entered = semaphore.WaitOne(Patience);
            try
            {
                if (!_backups.Contains(bk))
                {
                    bk.LocalID = ++ID;
                    _backups.Add(bk);
                }

                List<Task> tasks = new List<Task>();
                if (DefaultOptions.ShouldUploadApi && client != null)
                    tasks.Add(saveBkApi(bk));
                if (DefaultOptions.UploadSFTP)
                    tasks.Add(saveBkSftp(bk));
                tasks.Add(saveBkLocally(bk));
                await Task.WhenAll(tasks);
                arrayChanged();
            }
            catch(Exception ex)
            {
                SmbLog.Error("Problém při přidávání info o záloze", ex, LogCategory.BackupInfoManager);
            }
            finally
            {
                if (entered)
                    semaphore.Release();
            }
        }

        /// <summary>
        /// Přidá zálohu a nastaví jí správné Id, ale zatím ji nikam neukládá - proto na ní zavolej Add potom, až bude hodná uložení
        /// </summary>
        /// <param name="bk"></param>
        public Task AddQuietlyAsync(Backup bk)
        {
            return Task.Run(() =>
            {
                bool entered = semaphore.WaitOne(Patience);
                try
                {
                    bk.LocalID = ++ID;
                    _backups.Add(bk);
                }
                finally
                {
                    if (entered)
                        semaphore.Release();
                }
            });
        }

        /// <summary>
        /// Odstraní info o záloze
        /// </summary>
        /// <param name="bk"></param>
        public async Task DeleteAsync(Backup bk, SftpUploader sftp = null)
        {
            bool entered = semaphore.WaitOne(Patience);
            try
            {
                _backups.RemoveAll(b => b.LocalID == bk.LocalID);

                var del_local = deleteBkLocally(bk);
                var del_sftp = DefaultOptions.UploadSFTP ? deleteBkSftp(bk, sftp) : Task.CompletedTask;
                var del_api = DefaultOptions.ShouldUploadApi ? deleteBkApi(bk) : Task.CompletedTask;

                await Task.WhenAll(del_local, del_sftp, del_api);
            }
            catch (Exception ex)
            {
                SmbLog.Error("Problém při ostraňování info o záloze", ex, LogCategory.BackupInfoManager);
            }
            finally
            {
                if (entered)
                    semaphore.Release();
            }
        }

        /// <summary>
        /// Updatuje info o záloze
        /// </summary>
        /// <param name="bk"></param>
        public async Task UpdateAsync(Backup bk, SftpUploader sftp = null)
        {
            bool entered = semaphore.WaitOne(Patience);
            try
            {
                var ind = _backups.FindIndex(b => b.LocalID == bk.LocalID);
                if (ind >= 0 && _backups[ind] != bk)
                    _backups[ind] = bk;

                var del_local = updateBkLocally(bk);
                var del_sftp = DefaultOptions.UploadSFTP ? updateBkSftp(bk, sftp) : Task.CompletedTask;
                var del_api = DefaultOptions.ShouldUploadApi ? updateBkApi(bk) : Task.CompletedTask;

                await Task.WhenAll(del_local, del_sftp, del_api);
            }
            catch (Exception ex)
            {
                SmbLog.Error("Problém při updatování info o záloze", ex, LogCategory.BackupInfoManager);
            }
            finally
            {
                if (entered)
                    semaphore.Release();
            }
        }

        private async Task<IEnumerable<Backup>> listBksApi()
        {
            if (plans?.PlanInfo == null)
                return Enumerable.Empty<Backup>();

            try
            {
                var baks = await client.ListBackupsAsync(plans.PlanInfo.ID);
                //var pc_id = SMB_Utils.GetComputerId();
                return baks;
            }
            catch (Exception ex)
            {
                return Enumerable.Empty<Backup>();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sftp">tato metoda nevolá connect</param>
        /// <returns></returns>
        private async Task<IEnumerable<Backup>> listBksSftp(SftpUploader sftpUploader, Func<PC_Info, bool> filter, Dictionary<string, string> paths)
        {
            List<Backup> found = new List<Backup>();
            try
            {
                //projít všechny PC na serveru
                foreach (var info in SftpMetadataManager.GetPCInfos(sftpUploader))
                {
                    if (!filter(info)) //pokud toto PC neodpovídá filtru, kašlat naň
                        continue;

                    //získat všechny soubory ve složce
                    var files = sftpUploader.ListDir(SMB_Utils.GetRemoteBkinfosPath(info.RemoteFolderName).NormalizePath(),
                        false, file => file.IsRegularFile); 

                    foreach (var file in files.Select(pair => pair.Value)) //projít je
                    {
                        try
                        {
                            //vytáhnout text ze souboru, deserializovati jej a přidat do seznamu pro vrácení
                            var xml = sftpUploader.client.ReadAllText(file.FullName);
                            var deserialized = Backup.DeXml(xml, null);
                            found.Add(deserialized);

                            //zapamatovat si, odkud jsme soubor vzali
                            paths?.Add(deserialized.UniqueHash, file.FullName);
                        }
                        catch (Exception ex)
                        {
                            SmbLog.Debug($"Nepodařilo se načíst soubor s informacemi o záloze přes SFTP\ncesta: \"{file.FullName}\"", ex, LogCategory.BackupInfoManager);
                        }

                    }
                }

                return found;
            }
            catch (Exception ex)
            {
                SmbLog.Error("Nepodařilo se stáhnout info o zálohách ze sftp.", ex, LogCategory.BackupInfoManager);
                return Enumerable.Empty<Backup>();
            }

            //následuje zastaralý kód, který vracel jen info o zálohách z aktuálního PC
            //kód byl nahrazen kódem, který umí vracet i info o zálohách z jiných PC

            //try
            //{
            //    List<Backup> found = new List<Backup>();

            //    var folder = SMB_Utils.GetRemoteBkinfosPath().NormalizePath();
            //    sftpUploader.CreateDirectory(folder);
            //    var sftp = sftpUploader.client;
                
            //    var bk_list_async = sftp.BeginListDirectory(folder, null, null);
            //    var files = await Task.Factory.FromAsync(bk_list_async, sftp.EndListDirectory);                
            //    foreach(var file in files)
            //    {
            //        if (file.Name == "." || file.Name == "..")
            //            continue;

            //        var content = sftp.ReadAllText(file.FullName);
            //        try
            //        {
            //            found.Add(Backup.DeXml(content));
            //        }
            //        catch { }
            //    }

            //    return found;
            //}
            //catch (Exception ex)
            //{
            //    return Enumerable.Empty<Backup>();
            //}       
        }

        private async Task<IEnumerable<Backup>> listBksLocal(Dictionary<string, string> paths)
        {
            return await Task.Run(() =>
            {
                List<Backup> found = new List<Backup>();

                var folder = Const.BK_INFOS_FOLDER;
                Directory.CreateDirectory(folder);
                foreach (var file in Directory.GetFiles(folder, "*.xml"))
                {
                    var content = File.ReadAllText(file);
                    try
                    {
                        var bk = Backup.DeXml(content, file);
                        found.Add(bk);
                        paths.Add(bk.UniqueHash, file); //přidat cestu do slovníku
                    }
                    catch { }
                }

                return found;
            });
        }

        private async Task<bool> saveBkApi(Backup bk)
        {
            try
            {
                await client.AddBackupAsync(bk, plans.PlanInfo.ID);
                return true;
            }
            catch (Exception ex)
            {
                SmbLog.Error("Problém při nahrávání info o záloze do webové aplikace", ex, LogCategory.BackupInfoManager);
                return false;
            }
        }

        private async Task<bool> saveBkSftp(Backup bk, SftpUploader sftp = null)
        {
            bool disc = false; //jestli se na konci metody odpojit
            if (sftp == null)
            {
                sftp = Manager.Get<SftpUploader>();
                disc = true;
                if((await sftp.TryConnectAsync(2000)) != true)
                    return false;
            }

            string fpath = Path.Combine(SMB_Utils.GetRemoteBkinfosPath(), bk.GenInfoFileName(false)).NormalizePath();
            return await Task.Run<bool>(() =>
            {
                try
                {
                    sftp.CreateDirectory(SMB_Utils.GetRemoteBkinfosPath().NormalizePath());
                    using (var writer = new StreamWriter(sftp.client.Create(fpath)))
                        writer.Write(bk.ToXml());
                    return true;
                }
                catch (Exception ex)
                {
                    SmbLog.Error("Problém při nahrávání info o záloze na SFTP server", ex, LogCategory.BackupInfoManager);
                    return false;
                }
                finally
                {
                    if (disc)
                        try
                        {
                            if (sftp.IsConnected)
                                sftp.Disconnect();
                            sftp.Dispose();
                        }
                        catch { }
                }
            });
        }

        private async Task<bool> saveBkLocally(Backup bk)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string xml = bk.ToXml();

                    //název souboru - bude obsahovat ID počítače pouze, pokud záloha nebyla vytvořena na tomto PC
                    string path = Path.Combine(Const.BK_INFOS_FOLDER, bk.GenInfoFileName());
                    File.WriteAllText(path, xml);
                    return true;
                }
                catch (Exception ex)
                {
                    SmbLog.Error("Problém při ukládání info o záloze do souboru", ex, LogCategory.BackupInfoManager);
                    return false;
                }
            });
        }

        private Task<bool> deleteBkLocally(Backup bk)
        {
            return Task.Run(() =>
            {
                try
                {
                    File.Delete(Path.Combine(Const.BK_INFOS_FOLDER, bk.GenInfoFileName()));
                    return true;
                }
                catch
                {
                    return false;
                }
            });
        }

        private async Task<bool> deleteBkApi(Backup bk)
        {
            if (plans.PlanInfo == null) return false;

            try
            {
                var planId = plans.PlanInfo.ID;
                await client.DeleteBackupAsync(bk.LocalID, planId);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> deleteBkSftp(Backup bk, SftpUploader sftp = null)
        {
            bool disc = false; //jestli se na konci metody odpojit
            if (sftp == null)
            {
                sftp = Manager.Get<SftpUploader>();
                disc = true;
                if ((await sftp.TryConnectAsync(2000)) != true)
                    return false;
            }

            string fpath = Path.Combine(SMB_Utils.GetRemoteBkinfosPath(), bk.GenInfoFileName());
            return await Task.Run<bool>(() =>
            {
                try
                {
                    sftp.GetFile(fpath).Delete();
                    return true;
                }
                catch (Exception ex)
                {
                    SmbLog.Error("Problém při odstraňování info o záloze ze SFTP serveru", ex, LogCategory.BackupInfoManager);
                    return false;
                }
                finally
                {
                    if (disc)
                        try
                        {
                            if (sftp.IsConnected)
                                sftp.Disconnect();
                            sftp.Dispose();
                        }
                        catch { }
                }
            });
        }

        private async Task<bool> updateBkLocally(Backup bk)
        {
            var path = Path.Combine(Const.BK_INFOS_FOLDER, bk.GenInfoFileName(true));

            return await Task.Run(() =>
            {
                try
                {
                    File.WriteAllText(path, bk.ToXml());
                    return true;
                }
                catch
                {
                    return false;
                }
            });
        }

        private async Task<bool> updateBkApi(Backup bk)
        {
            if (plans.PlanInfo == null) return false;

            try
            {
                var planId = plans.PlanInfo.ID;
                await client.UpdateBackupAsync(bk, planId);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> updateBkSftp(Backup bk, SftpUploader sftp = null)
        {
            bool disc = false; //jestli se na konci metody odpojit
            if (sftp == null)
            {
                sftp = Manager.Get<SftpUploader>();
                disc = true;
                if ((await sftp.TryConnectAsync(2000)) != true)
                    return false;
            }

            string fpath = Path.Combine(SMB_Utils.GetRemoteBkinfosPath(), bk.GenInfoFileName(false));
            return await Task.Run<bool>(() =>
            {
                try
                {
                    var file = sftp.GetFile(fpath);
                    if (file != null) sftp.DeleteFile(fpath);
                    sftp.client.WriteAllText(fpath, bk.ToXml());
                    return true;
                }
                catch (Exception ex)
                {
                    SmbLog.Error("Problém při updatování info o záloze na SFTP serveru", ex, LogCategory.BackupInfoManager);
                    return false;
                }
                finally
                {
                    if (disc)
                        try
                        {
                            if (sftp.IsConnected)
                                sftp.Disconnect();
                            sftp.Dispose();
                        }
                        catch { }
                }
            });
        }

        /// <summary>
        /// Projde zálohy vytvořené na tomto PC a zařídí, aby informace o nich uložené
        /// (lokálně, na sftp, na webu) používaly aktuální typ id (SMB_Utils.ID_TYPE_TO_USE)
        /// </summary>
        /// <returns></returns>
        public async Task FixIDs()
        {
            //TODO: otestovat FixIDs

            SmbLog.Info("FixIDs zavoláno", null, LogCategory.Service);

            //projít zálohy z tohoto PC
            foreach (var bk in LocalBackups.Where(b => b.IdType != SMB_Utils.ID_TYPE_TO_USE))
            {
                bk.IdType = SMB_Utils.ID_TYPE_TO_USE; //nastavit aktuální typ id
                bk.ComputerId = SMB_Utils.GetComputerId(); //nastavit id podle aktuálního typu

                //updatovat info o záloze lokálně, na sftp, a přes api
                await Task.WhenAll
                (
                                                updateBkLocally(bk),
                    DefaultOptions.UploadSFTP ? updateBkSftp(bk)     : Task.CompletedTask, 
                    DefaultOptions.UploadApi  ? updateBkApi(bk)      : Task.CompletedTask
                );
            }
        }

        private string idpath => Path.Combine(Const.BK_INFOS_FOLDER, "id");

        /// <summary>
        /// Počítač id. Při každém přidání zálohy se zvýší o 1;
        /// </summary>
        public int ID
        {
            get
            {
                if (!File.Exists(idpath))
                    return LocalBackups.Any() ? LocalBackups.Max(f => f.LocalID) : 1;
                else
                {
                    try
                    {
                        int val1 = 1;
                        if (File.Exists(idpath))
                        {
                            File.SetAttributes(idpath, FileAttributes.Normal);
                            val1 = int.Parse(File.ReadAllText(idpath));
                            File.SetAttributes(idpath, FileAttributes.Hidden);
                        }

                        int val2 = LocalBackups.Any() ? LocalBackups.Max(f => f.LocalID) : 1;
                        return Math.Max(val1, val2);
                    }
                    catch
                    {
                        return LocalBackups.Any() ? LocalBackups.Max(f => f.LocalID) : 1;
                    }
                }
            }
            private set
            {
                if (File.Exists(idpath))
                    File.SetAttributes(idpath, FileAttributes.Normal);
                else
                    Directory.CreateDirectory(Path.GetDirectoryName(idpath));
                File.WriteAllText(idpath, value.ToString());
                File.SetAttributes(idpath, FileAttributes.Hidden);
            }
        }
    }
}
