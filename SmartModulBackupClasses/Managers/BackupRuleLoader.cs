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
    /// <summary>
    /// Spravuje zálohovací pravidla
    /// </summary>
    public class BackupRuleLoader : INotifyPropertyChanged
    {
        const string deleteSave = "rules_to_delete.txt";

        //nechcem do api posílat víc příkazů najednou
        TaskQueue apiQueue = new TaskQueue();
        private SmbApiClient client => Manager.Get<AccountManager>()?.Api;
        private readonly string folder;

        public BackupRuleLoader()
        {
            this.folder = Const.RULES_FOLDER;
        }

        private List<BackupRule> ruleList = new List<BackupRule>();

        //PropertyChanged je třeba invokovat přes Dispatcher, aby to nemělo kecy
        public Action<Action> UI_Dispatcher;

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<BackupRule> OnRuleUpdated;

        /// <summary>
        /// Zdali stahovat updatovaná pravidla přes api; byl bych s tím opatrný, může to být bezpečnostní riziko dle
        /// mého skromného názoru
        /// </summary>
        public bool Download { get; set; } = false;

        public BackupRule[] Rules => ruleList.ToArray();

        public void InvokeDatabaseSourceUpdate()
        {
            var ruleDatabaseSourceUpdater = Manager.Get<NewDatabaseHandler>();

            ruleDatabaseSourceUpdater.CheckForNewDatabasesAndUpdateRulesAccordingly(Rules, rule => Update(rule), 
                Manager.Get<ConfigManager>().Config.EmailConfig.SendNewDatabases);            
        }

        public bool TryInvokeDatabaseSourceUpdate()
        {
            try
            {
                // u pravidel, které mají zaškrtnuto automatické přidávání nových databází, automaticky přidat nové databáze
                InvokeDatabaseSourceUpdate();
                return true;
            }
            catch (Exception ex)
            {
                SmbLog.Error("problém s přidáním nových databází do pravidla", ex, LogCategory.BackupRuleLoader);
                return false;
            }
        }

        private void rulesChanged()
        {
            TryInvokeDatabaseSourceUpdate();

            var handler = UI_Dispatcher ?? new Action<Action>(a => a());
            handler.Invoke(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Rules))));
        }

        /// <summary>
        /// Zavolá OnRuleUpdated pomocí UI_Dispatcheru.
        /// </summary>
        /// <param name="updated"></param>
        private void invokeRuleUpdated(BackupRule updated)
        {
            var handler = UI_Dispatcher ?? new Action<Action>(a => a());
            handler.Invoke(() => OnRuleUpdated?.Invoke(this, updated));
        }

        public bool Loading { get; private set; }

        /// <summary>
        /// Načte aktuální pravidla ze souborů a z webu
        /// </summary>
        /// <returns>Odkaz na tento objekt</returns>
        public BackupRuleLoader Load()
        {
            if (Loading) { return this; }

            try
            {
                Loading = true;

                var newRuleList = new List<BackupRule>(); // seznam načtených pravidel

                var rulesLocal = loadFromFiles().ToList(); //lokálně uložená pravidla

                try //může dojít k chybě v api (např. pokud k api vůbec nejsme připojeni), proto try
                {
                    var rulesWeb = client.GetBackupRules();

                    //nejprve projdeme info o pravidlech stažených z webu
                    foreach (var w_rule in rulesWeb)
                    {
                        //pro každé se pokusíme najít příslušné lokální pravidlo
                        var l_rule = rulesLocal.FirstOrDefault(f => f.LocalID == w_rule.LocalID);

                        //pokud existuje lokální pravidlo
                        if (l_rule != null)
                        {
                            //pokud má lokální pravidlo datum změny po webové verzi, znamená to, že byly provedeny lokální změny
                            if (l_rule.LastEdit > w_rule.LastEdit)
                            {
                                newRuleList.Add(l_rule); //přidat pravidlo do seznamu
                                apiQueue.Enqueue(() => client.UpdateRulesAsync(l_rule)); //musíme o tom informovat server
                            }
                            //pokud jsme změnili pravidlo na webu a zároveň je zapnuté stahování updatovaných pravidel
                            else if (l_rule.LastEdit < w_rule.LastEdit && Download)
                            {
                                w_rule.path = l_rule.path;
                                newRuleList.Add(w_rule); //přidat pravidlo do seznamu
                                w_rule.SaveSelf();
                            }
                            //jinak by měla být webová verze shodná s lokální, takže prostě přidáme l_rule na seznam
                            else
                                newRuleList.Add(l_rule);
                        }
                        //pokud neexistuje
                        else
                        {
                            //pokud pravidlo již bylo staženo, znamená to, že bylo lokálně smazáno, smažeme ho tedy i na webu
                            if (w_rule.Downloaded)
                                apiQueue.Enqueue(() => client.DeleteRulesAsync(w_rule.LocalID));
                            //jinak to znamená, že pravidlo bylo vytvořeno na serveru, stáhneme ho pouze, pokud je stahování zapnuto
                            else if (Download)
                            {
                                w_rule.path = Path.Combine(folder, w_rule.Name + ".xml");
                                newRuleList.Add(w_rule); //přidat pravidlo do seznamu

                                //nastavit downloaded na serveru, ať víme, že pravidlo již bylo staženo
                                apiQueue.Enqueue(() => client.ConfirmRulesAsync(w_rule.LocalID));
                            }
                        }
                    }

                    //projít lokální pravidla
                    foreach (var l_rule in rulesLocal)
                    {
                        if (newRuleList.Any(f => f.LocalID == l_rule.LocalID))
                            continue;
                        //pokračujeme pouze, pokud toto pravidlo není ve výsledném seznamu pravidel

                        //pokud pravidlo bylo již uploadováno, znamená to, že bylo smazáno na serveru, smažeme ho tedy i lokálně
                        if (l_rule.Uploaded && Download)
                        {
                            try
                            {
                                File.Delete(l_rule.path);
                            }
                            catch { }
                            continue;
                        }
                        //jinak ho prostě přidáme a informujeme server o novém pravidlu
                        else
                        {
                            newRuleList.Add(l_rule); //přidat pravidlo do seznamu
                            apiQueue.Enqueue(() => client.UpdateRulesAsync(l_rule));
                            l_rule.Uploaded = true;
                        }
                    }
                }
                //pokud dojde k chybě při komunikaci s webem (nebo nějaké jiné chybě), prostě vezmeme jen lokální pravidla
                catch (Exception ex)
                {
                    if (!(ex is NullReferenceException)) //NullReferenceException znamená, že k api nejsme připojeni, a to se nepočítá jako chyba
                        SmbLog.Error("Chyba při načítání pravidel z webové aplikace. Načítám pouze lokálně uložená pravidla.", ex, LogCategory.BackupRuleLoader);

                    newRuleList.Clear();
                    newRuleList.AddRange(rulesLocal); // přidat lokálně načtený pravidla do seznamu
                }

                //updatovat stávající seznam podle nového seznamu
                ruleList.UpdateCollectionByCompare(newRuleList, (r1, r2) => r1.LocalID == r2.LocalID);

                rulesChanged();
                return this;
            }
            finally
            {
                Loading = false;
            }
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

        /// <summary>
        /// Vrátí BackupRules uložené ve složce jako xml
        /// </summary>
        /// <returns></returns>
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
            if (ruleList.Any(r => r.Name == rule.Name))
                throw new InvalidOperationException($"Pravidla musí mít unikátní názvy! Název {rule.Name} je již zabrán.");

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
            if (ruleList.Any(r => r.Name == rule.Name && r.LocalID != rule.LocalID))
                throw new InvalidOperationException($"Pravidla musí mít unikátní názvy! Název {rule.Name} je již zabrán.");

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

        /// <summary>
        /// Jestli alespoň jedno pravidlo splňuje funkci
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        public bool Any(Func<BackupRule, bool> func)
        {
            return ruleList.Any(func);
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
            catch (Exception ex)
            {
                SmbLog.Error($"Problém při načítání pravidla v umístění {path}", ex, LogCategory.BackupRuleLoader);
                return null;
            }
        }
    }
}
