using Renci.SshNet;
using SmartModulBackupClasses.WebApi;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartModulBackupClasses.Managers
{
    public class BackupInfoManager : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private List<Backup> backups = new List<Backup>();
        public Backup[] Backups
        {
            get => backups.ToArray();
        }
        public IEnumerable<Backup> LocalBackups
        {
            get
            {
                var pc_id = SMB_Utils.GetComputerId();
                return Backups.Where(f => f.ComputerId == pc_id);
            }
        }

        SmbApiClient client => Manager.Get<SmbApiClient>();
        readonly ConfigManager config;
        readonly PlanManager plans;
        public BackupInfoManager()
        {
            config = Manager.Get<ConfigManager>();
            plans = Manager.Get<PlanManager>();
        }

        public bool UseApi { get; set; } = true;
        public bool UseSftp { get; set; } = true;

        void arrayChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Backups)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LocalBackups)));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sync">pokud true, budou se doplňovat chybějící informace na všechny zvolené strany (lokálně / api / sftp)</param>
        /// <returns></returns>
        public async Task LoadAsync(bool sync = true)
        {
            backups.Clear();

            IEnumerable<Backup> apiResults = null; //zde budou všechny zálohy z aktivovaného plánu (čili mohou být i z jiných počítačů)
            IEnumerable<Backup> sftpResults = null;
            IEnumerable<Backup> localResults = null;

            List<Task> listTasks = new List<Task>();

            if (UseApi)
                listTasks.Add(Task.Run(async () => apiResults = await listBksApi()));
            else
                apiResults = Enumerable.Empty<Backup>();

            SftpUploader sftp = null;
            if (UseSftp)
            {
                sftp = Manager.Get<SftpUploader>();
                listTasks.Add(Task.Run(async () =>
                {
                    if ((await sftp.TryConnectAsync(2000)) == true)
                    {
                        sftpResults = await listBksSftp(sftp);
                    }
                    else
                    {
                        sftp = null;
                        sftpResults = Enumerable.Empty<Backup>();
                    }
                }));
            }
            else
                sftpResults = Enumerable.Empty<Backup>();

            listTasks.Add(Task.Run(async () => localResults = await listBksLocal()));

            await Task.WhenAll(listTasks);

            var pc_id = SMB_Utils.GetComputerId();

            backups.AddRange(apiResults);

            Func<Backup, bool> canAdd = new Func<Backup, bool>(bk =>
            {
                if (!bk.MadeOnThisComputer)
                    return true;
                return !backups.Any(exbk => exbk.LocalID == bk.LocalID);
            });

            backups.AddRange(sftpResults.Where(canAdd)); 
            backups.AddRange(localResults.Where(canAdd));

            foreach (var bk in backups.Where(f => f.AvailableOnThisComputer))
                bk.CheckLocalAvailibility();

            List<Task> sftpTasks = new List<Task>();
            if (sync)
            {
                //synchronizace - projdeme všechny zálohy a ujistíme se, aby všechny byly nahrané na všech třech stranách
                foreach(var bk in backups.Where(f=>f.AvailableOnThisComputer))
                {
                    if (!apiResults.Any(f => f.LocalID == bk.LocalID) && UseApi && client != null)
                        _ = saveBkApi(bk);
                    if (!sftpResults.Any(f => f.LocalID == bk.LocalID) && UseSftp && sftp != null)
                        sftpTasks.Add(saveBkSftp(bk, sftp));
                    if (!localResults.Any(f => f.LocalID == bk.LocalID))
                        _ = saveBkLocally(bk);
                }
            }

            _ = Task.WhenAll(sftpTasks).ContinueWith(task => sftp?.Disconnect());

            arrayChanged();
        }

        public void Load(bool sync = true) => SMB_Utils.Sync(() => LoadAsync(sync));

        /// <summary>
        /// Přidá info o záloze: uloží ho lokálně, popř. na SFTP, popř. na WEB
        /// </summary>
        /// <param name="bk"></param>
        public void Add(Backup bk)
        {
            if (!backups.Contains(bk))
            {
                bk.LocalID = ++ID;
                backups.Add(bk);
            }

            if (UseApi && client != null)
                _ = saveBkApi(bk);
            if (UseSftp)
                _ = saveBkSftp(bk);
            saveBkLocally(bk).Wait();
            arrayChanged();
        }

        /// <summary>
        /// Přidá zálohu a nastaví jí správné Id, ale zatím ji nikam neukládá - proto na ní zavolej Add potom, až bude hodná uložení
        /// </summary>
        /// <param name="bk"></param>
        public void AddQuietly(Backup bk)
        {
            bk.LocalID = ++ID;
            backups.Add(bk);
        }


        private async Task<IEnumerable<Backup>> listBksApi()
        {
            try
            {
                var baks = await client.ListBackupsAsync(plans.Plan.ID);
                var pc_id = SMB_Utils.GetComputerId();
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

                var sftp = sftpUploader.client;
                var bk_list_async = sftp.BeginListDirectory(SMB_Utils.GetRemoteBkinfosPath(), null, null);
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
                await client.AddBackupAsync(bk, plans.Plan.ID);
                return true;
            }
            catch (Exception ex)
            {
                SMB_Log.Log(ex);
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
                    SMB_Log.Log(ex);
                    return false;
                }
                finally
                {
                    if (disc)
                        try
                        {
                            sftp.Disconnect();
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
                    SMB_Log.Log(ex);
                    return false;
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
