﻿using SmartModulBackupClasses.WebApi;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartModulBackupClasses.Managers
{
    public class BackupRuleLoader : INotifyPropertyChanged
    {
        const string deleteSave = "rules_to_delete.txt";

        TaskQueue apiQueue = new TaskQueue();
        private SmbApiClient client => Manager.Get<SmbApiClient>();
        private readonly string folder;

        public BackupRuleLoader()
        {
            this.folder = Const.RULES_FOLDER;
        }

        private List<BackupRule> ruleList = new List<BackupRule>();

        public Action<Action> UI_Dispatcher;

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<BackupRule> OnRuleUpdated;

        public BackupRule[] Rules => ruleList.ToArray();

        private void rulesChanged()
        {
            var handler = UI_Dispatcher ?? new Action<Action>(a => a());
            handler.Invoke(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Rules))));
        }

        private void invokeRuleUpdated(BackupRule updated)
        {
            var handler = UI_Dispatcher ?? new Action<Action>(a => a());
            handler.Invoke(() => OnRuleUpdated?.Invoke(this, updated));
        }

        /// <summary>
        /// Načte aktuální pravidla ze souborů a z webu
        /// </summary>
        /// <returns>Odkaz na tento objekt</returns>
        public BackupRuleLoader Load()
        {
            ruleList.Clear();

            var rulesLocal = loadFromFiles().ToList();

            try //může dojít k chybě v api, proto try
            {
                var rulesWeb = client.GetBackupRules();

                foreach (var w_rule in rulesWeb)
                {
                    var l_rule = rulesLocal.FirstOrDefault(f => f.LocalID == w_rule.LocalID);
                    if (l_rule != null)
                    {
                        if (l_rule.LastEdit >= w_rule.LastEdit) //pokud má lokální pravidlo datum změny po webové verzi, znamená to, že byly provedeny lokální změny
                        {
                            ruleList.Add(l_rule);
                            apiQueue.Enqueue(() => client.UpdateRulesAsync(l_rule)); //musíme o tom informovat server
                        }
                        else
                        {
                            w_rule.path = l_rule.path;
                            ruleList.Add(w_rule);
                        }
                    }
                    else
                    {
                        if (w_rule.Downloaded) //pokud pravidlo již bylo staženo, znamená to, že bylo lokálně smazáno, smažeme ho tedy i na webu
                            apiQueue.Enqueue(() => client.DeleteRulesAsync(w_rule.LocalID));
                        else
                        {
                            w_rule.path = Path.Combine(folder, w_rule.Name + ".xml");
                            ruleList.Add(w_rule);
                            apiQueue.Enqueue(() => client.ConfirmRulesAsync(w_rule.LocalID)); //přidat: nastaví Downloaded na serveru
                        }
                    }
                }

                foreach (var l_rule in rulesLocal)
                {
                    if (ruleList.Any(f => f.LocalID == l_rule.LocalID)) 
                        continue;
                    //pokračujeme pouze, pokud toto pravidlo není ve výsledném seznamu pravidel

                    if (l_rule.Uploaded) //pokud pravidlo bylo již uploadováno, znamená to, že bylo smazáno na serveru, smažeme ho tedy i lokálně
                    {
                        try
                        {
                            File.Delete(l_rule.path);
                        }
                        catch { }
                        continue;
                    }
                    else
                    {
                        ruleList.Add(l_rule);
                        apiQueue.Enqueue(() => client.UpdateRulesAsync(l_rule));
                        l_rule.Uploaded = true;
                    }
                }
            }
            catch (Exception ex) //pokud dojde k chybě při komunikaci s webem, prostě vezmeme jen lokální pravidla
            {
                SMB_Log.Log(ex);
                ruleList.Clear();
                ruleList.AddRange(rulesLocal);
            }

            rulesChanged();
            return this;
        }

        //private bool tryDownloadRules(List<BackupRule> outputList)
        //{
        //    if (client == null)
        //        return false;

        //    try
        //    {
        //        outputList.AddRange(client.GetBackupRules());
        //        return true;
        //    }
        //    catch
        //    {
        //        return false;
        //    }
        //}

        private IEnumerable<BackupRule> loadFromFiles()
        {
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
                yield break;
            }

            var files = Directory.GetFiles(folder);

            foreach(var path in files)
            {
                if (Path.GetExtension(path) != ".xml")
                    continue;

                var rule = tryGetRule(path);
                if (rule != null)
                    yield return rule;
            }
        }

        /// <summary>
        /// Vrátí pravidlo podle lokálního ID.
        /// </summary>
        /// <param name="localID"></param>
        /// <returns></returns>
        public BackupRule Get(int localID)
        {
            return ruleList.FirstOrDefault(f => f.LocalID == localID);
        }

        /// <summary>
        /// Přidá pravidlo, uloží do souboru, upozorní API.
        /// </summary>
        /// <param name="rule"></param>
        /// <returns></returns>
        public BackupRule Add(BackupRule rule)
        {
            rule.LocalID = ++ID;
            rule.LastEdit = DateTime.Now;
            ruleList.Add(rule);
            apiQueue.Enqueue(() => ruleUpdate(rule));
            rule.path = Path.Combine(folder, rule.Name.Replace(" ", "_") + ".xml");
            rule.SaveSelf();

            rulesChanged();
            invokeRuleUpdated(rule);
            return rule;
        }

        /// <summary>
        /// Updatuje záznam pravidla v seznamu, updatuje soubor a upozorní API.
        /// </summary>
        /// <param name="rule"></param>
        /// <returns></returns>
        public BackupRule Update(BackupRule rule)
        {
            var f_rule = ruleList.FirstOrDefault(f => f.LocalID == rule.LocalID);
            if (f_rule == null)
                throw new InvalidOperationException("Nelze updatovat neexistující pravidlo.");
            else
            {
                if (f_rule != rule)
                    ruleList[ruleList.IndexOf(f_rule)] = rule;

                rule.LastEdit = DateTime.Now;
                rule.SaveSelf();
                apiQueue.Enqueue(() => ruleUpdate(rule));
            }

            invokeRuleUpdated(rule);
            return rule;
        }

        private async Task ruleUpdate(BackupRule rule)
        {
            if (client == null)
                return;

            try
            {
                await client.UpdateRulesAsync(rule);
                rule.Uploaded = true;
                rule.SaveSelf();
            }
            catch { }
        }

        /// <summary>
        /// Odstraní pravidlo ze seznamu, smaže soubor a upozorní API.
        /// </summary>
        /// <param name="local_id"></param>
        /// <returns></returns>
        public BackupRule Delete(int local_id)
        {
            var rule = ruleList.FirstOrDefault(f => f.LocalID == local_id);
            if (rule == null)
                throw new InvalidOperationException("Pravidlo s daným LocalID neexistuje!");

            ruleList.Remove(rule);
            apiQueue.Enqueue(() => client?.DeleteRulesAsync(local_id));
            if (rule.path != null && File.Exists(rule.path))
                File.Delete(rule.path);

            rulesChanged();
            return rule;
        }

        /// <summary>
        /// Přidá / nastaví dané pravidlo u tohoto objektu. Nezabývá se komunikací s API ani soubory pravidel.
        /// </summary>
        /// <param name="rule"></param>
        /// <returns></returns>
        public BackupRule SetRule(BackupRule rule)
        {
            int i = ruleList.FindIndex(r => r.LocalID == rule.LocalID);
            if (i >= 0)
                ruleList[i] = rule;
            else
                ruleList.Add(rule);

            rulesChanged();
            invokeRuleUpdated(rule);
            return rule;
        }

        private string idpath => Path.Combine(folder, "id");

        /// <summary>
        /// Počítač id. Při každém přidání pravidla se zvýší o 1;
        /// </summary>
        public int ID
        {
            get
            {
                if (!File.Exists(idpath))
                    return ruleList.Any() ? ruleList.Max(f => f.LocalID) : 1;
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

                        int val2 = ruleList.Any() ? ruleList.Max(f => f.LocalID) : 1;
                        return Math.Max(val1, val2);
                    }
                    catch
                    {
                        return ruleList.Any() ? ruleList.Max(f => f.LocalID) : 1;
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

        private BackupRule tryGetRule(string path)
        {
            try
            {
                return BackupRule.LoadFromXml(path);
            }
            catch
            {
                return null;
            }
        }
    }
}