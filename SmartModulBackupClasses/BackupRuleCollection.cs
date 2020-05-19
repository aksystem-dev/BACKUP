using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections.Specialized;
using System.Collections;
using System.IO;

namespace SmartModulBackupClasses
{
    //  ###         ###  ###   ######
    //   ###       ###   ###   ###  ###
    //    ### ### ###    ###   ######
    //     #########     ###   ###
    //      ### ###      ###   ###

    /// <summary>
    /// WIP
    /// </summary>
    class BackupRuleCollection : INotifyPropertyChanged,
                                 INotifyCollectionChanged,
                                 ICollection<BackupRule>,
                                 IEnumerable<BackupRule>,
                                 IList<BackupRule>
    {
        string _ruleIdFile;
        string _ruleFolder;

        public int GetNextId()
        {
            if (!File.Exists(_ruleIdFile))
                return 1;

            return int.Parse(File.ReadAllText(_ruleIdFile) + 1);
        }

        public void Load(bool onlyValid = false)
        {

            rules.Clear();
            if (!Directory.Exists(_ruleFolder))
                Directory.CreateDirectory(_ruleFolder);
            else
            {
                var files = Directory.GetFiles(_ruleFolder, "*.xml");
                //načíst pravidla ze souborů
                if (files.Length > 0)
                    foreach (string rulepath in files)
                    {
                        //if (Path.GetExtension(rulepath) == ".xml")
                        {
                            try
                            {
                                var rule = BackupRule.LoadFromXml(rulepath);

                                //pravidlo přidáme na seznam, pokud je validní
                                if (!onlyValid || rule.Conditions.AllValid)
                                    rules.Add(rule);
                            }
                            catch
                            {
                                //Logger.Error($"Nepodařilo se načíst pravidlo umístěné v {rulepath}. Přeskakuji ho.\n\n{e}");
                            }
                        }
                    }
            }

            CollectionChanged?.Invoke(this,
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add | NotifyCollectionChangedAction.Reset));
        }

        public BackupRuleCollection(string ruleIdFile, string ruleFolder)
        {
            throw new NotImplementedException("WIP");

            _ruleIdFile = ruleIdFile;
            _ruleFolder = ruleFolder;
        }

        private List<BackupRule> rules = new List<BackupRule>();

        public BackupRule this[int index]
        {
            get => rules[index];
            set
            {
                rules[index] = value;
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace));
            }
        }

        public int Count => rules.Count;

        public bool IsReadOnly => false;

        public event PropertyChangedEventHandler PropertyChanged;
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public void Add(BackupRule item)
        {
            rules.Add(item);
        }

        public void Clear()
        {
            rules.Clear();
        }

        public bool Contains(BackupRule item)
        {
            return rules.Contains(item);
        }

        public void CopyTo(BackupRule[] array, int arrayIndex)
        {
            rules.CopyTo(array, arrayIndex);
        }

        public IEnumerator<BackupRule> GetEnumerator()
        {
            return rules.GetEnumerator();
        }

        public int IndexOf(BackupRule item)
        {
            return rules.IndexOf(item);
        }

        public void Insert(int index, BackupRule item)
        {
            rules.Insert(index, item);
        }

        public bool Remove(BackupRule item)
        {
            return rules.Remove(item);
        }

        public void RemoveAt(int index)
        {
            rules.RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
