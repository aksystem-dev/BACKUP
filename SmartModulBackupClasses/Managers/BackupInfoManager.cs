﻿using Renci.SshNet;
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

        private List<Backup> backups = new List<Backup>();

        /// <summary>
        /// Seznam všech načtených záloh.
        /// </summary>
        public Backup[] Backups
        {
            get => backups.ToArray();
        }

        /// <summary>
        /// Seznam načtených záloh, které byly vytvořeny na tomto počítači.
        /// </summary>
        public IEnumerable<Backup> LocalBackups
        {
            get
            {
                var pc_id = SMB_Utils.GetComputerId();
                return Backups.Where(f => f.ComputerId == pc_id);
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

        public bool UseApi { get; set; } = true;
        public bool UseSftp { get; set; } = false;

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

        /// <summary>
        /// Načte info:
        /// <para>   a) ze xml souborů v Const.BK_INFOS_FOLDER</para>
        /// <para>   b) pokud UseApi => z webového api (to vrátí všechny zálohy na aktuálním plánu, čili mohou být i z jiných
        ///     PC; pro filtr záloh jen z tohoto PC použijte LocalBackups)</para>
        /// <para>   c) pokud UseSftp => z SFTP serveru, kde mohou také být uloženy informace o zálohách v .xml formátu</para>
        /// </summary>
        /// <param name="sync">
        ///     pokud true, metoda se postará o to, aby na žádném ze zdrojů 
        ///     (lokální, pokud UseApi => webové api, pokud UseSftp => sftp server) 
        ///     nechybělo žádné info, které není na jiném zdroji
        /// </param>
        /// <returns></returns>
        public async Task LoadAsync(bool sync = true)
        {
            bool entered = semaphore.WaitOne(Patience);
            try
            {
                await _loadAsync(sync);
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
        private async Task _loadAsync(bool sync = true)
        {
            var newBackups = new List<Backup>();

            IEnumerable<Backup> apiResults = Enumerable.Empty<Backup>(); //zde budou všechny zálohy z aktivovaného plánu (čili mohou být i z jiných počítačů)
            IEnumerable<Backup> sftpResults = Enumerable.Empty<Backup>();
            IEnumerable<Backup> localResults = Enumerable.Empty<Backup>();

            List<Task> listTasks = new List<Task>();

            //používáme-li api, stáhnem z něj informace o zálohách
            if (UseApi)
                listTasks.Add(Task.Run(async () => apiResults = await listBksApi()));

            //také je možné, že jsou informace uložené na sftp;
            SftpUploader sftp = UseSftp ? Manager.Get<SftpUploader>() : null;
            if (sftp != null)
            {
                listTasks.Add(Task.Run(async () =>
                {
                    if ((await sftp?.TryConnectAsync(2000)) == true)
                        sftpResults = await listBksSftp(sftp);
                }));
            }

            //a konečně jsou také uložené lokálně
            listTasks.Add(Task.Run(async () => localResults = await listBksLocal()));

            //posečkáme, až všechny tři úlohy sklidí ovoce své píle
            await Task.WhenAll(listTasks);

            //teď přidáme do seznamu všechny získané informace. Budeme je přidávat postupně tak, aby nikdy nebylo
            //více záloh se stejným LocalID ze stejného počátače. Nejprve přidáme zálohy z api. Api vrací zálohy
            //nejen z volajícího počítače, ale ze všech počítačů napojených na jeho plán. Potom do toho zamícháme
            //i informace z lokálních souborů a souborů na sftp; ty jsou zaručeně jen z tohoto počítače.
            newBackups.AddRange(apiResults);

            //funkce, kterou se budou filtrovat přidané zálohy. 
            Func<Backup, bool> canAdd = new Func<Backup, bool>(bk =>
            {
                //pokud nebyla vytvořena na tomto počítači, kašlem na to, prostě ji přidáme
                if (!bk.MadeOnThisComputer) 
                    return true;

                //pokud byla vytvořena na tomto počítači, přidáme ji pouze, pokud jsme už nepřidali nějakou se stejným localID
                return !newBackups.Any(exbk => exbk.LocalID == bk.LocalID);
            });

            newBackups.AddRange(sftpResults.Where(canAdd)); 
            newBackups.AddRange(localResults.Where(canAdd));

            List<Task> sftpTasks = new List<Task>();
            List<Task> apiTasks = new List<Task>();
            List<Task> localTasks = new List<Task>();

            if (sync)
            { 
                //máme tři zdroje informací o zálohách - api, sftp a lokální soubory. Chceme, aby na všech zdrojích
                //byly všechny zálohy. o to se postaráme zde
                foreach (var bk in LocalBackups) 
                {
                    //pokud UseApi == true
                    //a zároveň narazíme na zálohu, která byla vytvořena k aktuálnímu plánu, ale webové api o ni neví,
                    //nahrajeme jí tam
                    if (UseApi && !apiResults.Any(f => f.LocalID == bk.LocalID) && client != null && bk.UploadedToCurrentPlan)
                        apiTasks.Add(saveBkApi(bk));

                    //pokud UseSftp == true
                    //a zároveň narazíme na zálohu, která byla nahrána na aktuální SFTP server ale nebylo o ní nahráno info,
                    //nahrajeme jí tam
                    if (UseSftp && !sftpResults.Any(f => f.LocalID == bk.LocalID) && sftp?.IsConnected == true)
                        sftpTasks.Add(saveBkSftp(bk, sftp));

                    //pokud info o záloze není uloženo lokálně, uložíme ho
                    if (!localResults.Any(f => f.LocalID == bk.LocalID))
                        localTasks.Add(saveBkLocally(bk));
                }
            }

            //až budou všechny sftp tasky hotové, chceme se od sftp odpojit
            await Task.WhenAll(sftpTasks).ContinueWith(task => sftp?.Disconnect(false));
            await Task.WhenAll(apiTasks);
            await Task.WhenAll(localTasks);

            //nastavit pole backups podle newBackups
            backups.UpdateCollectionByCompare(newBackups, (b1, b2) => b1.LocalID == b2.LocalID);

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
                if (!backups.Contains(bk))
                {
                    bk.LocalID = ++ID;
                    backups.Add(bk);
                }

                List<Task> tasks = new List<Task>();
                if (UseApi && client != null)
                    tasks.Add(saveBkApi(bk));
                if (UseSftp)
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
                    backups.Add(bk);
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
                backups.RemoveAll(b => b.LocalID == bk.LocalID);

                var del_local = deleteBkLocally(bk);
                var del_sftp = UseSftp ? deleteBkSftp(bk, sftp) : Task.CompletedTask;
                var del_api = UseApi ? deleteBkApi(bk) : Task.CompletedTask;

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
                var ind = backups.FindIndex(b => b.LocalID == bk.LocalID);
                if (ind >= 0 && backups[ind] != bk)
                    backups[ind] = bk;

                var del_local = updateBkLocally(bk);
                var del_sftp = UseSftp ? updateBkSftp(bk, sftp) : Task.CompletedTask;
                var del_api = UseApi ? updateBkApi(bk) : Task.CompletedTask;

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
        private async Task<IEnumerable<Backup>> listBksSftp(SftpUploader sftpUploader)
        {
            try
            {
                List<Backup> found = new List<Backup>();

                var folder = SMB_Utils.GetRemoteBkinfosPath();
                sftpUploader.CreateDirectory(folder);
                var sftp = sftpUploader.client;
                var bk_list_async = sftp.BeginListDirectory(folder, null, null);
                var files = await Task.Factory.FromAsync(bk_list_async, sftp.EndListDirectory);
                foreach(var file in files)
                {
                    var content = sftp.ReadAllText(file.FullName);
                    try
                    {
                        found.Add(Backup.DeXml(content));
                    }
                    catch { }
                }

                return found;
            }
            catch
            {
                return Enumerable.Empty<Backup>();
            }       
        }

        private async Task<IEnumerable<Backup>> listBksLocal()
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
                        found.Add(Backup.DeXml(content));
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

            string fpath = Path.Combine(SMB_Utils.GetRemoteBkinfosPath(), bk.BkInfoNameStr());
            return await Task.Run<bool>(() =>
            {
                try
                {
                    sftp.CreateDirectory(SMB_Utils.GetRemoteBkinfosPath());
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
                    string path = Path.Combine(Const.BK_INFOS_FOLDER, bk.BkInfoNameStr());
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
                    File.Delete(Path.Combine(Const.BK_INFOS_FOLDER, bk.BkInfoNameStr()));
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

            string fpath = Path.Combine(SMB_Utils.GetRemoteBkinfosPath(), bk.BkInfoNameStr());
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
            var path = Path.Combine(Const.BK_INFOS_FOLDER, bk.BkInfoNameStr());
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

            string fpath = Path.Combine(SMB_Utils.GetRemoteBkinfosPath(), bk.BkInfoNameStr());
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

        private string idpath => Path.Combine(Const.BK_INFOS_FOLDER, "id");

        /// <summary>
        /// Počítač id. Při každém přidání zálohy se zvýší o 1;
        /// </summary>
        public int ID
        {
            get
            {
                if (!File.Exists(idpath))
                    return backups.Any() ? backups.Max(f => f.LocalID) : 1;
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

                        int val2 = backups.Any() ? backups.Max(f => f.LocalID) : 1;
                        return Math.Max(val1, val2);
                    }
                    catch
                    {
                        return backups.Any() ? backups.Max(f => f.LocalID) : 1;
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
