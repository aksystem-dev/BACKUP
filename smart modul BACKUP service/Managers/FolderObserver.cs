using smart_modul_BACKUP_service.BackupExe;
using smart_modul_BACKUP_service.FolderStructure;
using SmartModulBackupClasses;
using SmartModulBackupClasses.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace smart_modul_BACKUP_service.Managers
{
    /// <summary>
    /// Spouští pravidla typu ProtectedFolder (hledí na složku, ve chvíli, kdy se v ní něco změní, spustí pravidlo)
    /// </summary>
    public class FolderObserver
    {
        public double Interval
        {
            get => _timer.Interval;
            set => _timer.Interval = value;
        }

        private Timer _timer = new Timer();

        public FolderObserver()
        {
            _timer.Interval = 5000;
            _timer.Elapsed += _timer_Elapsed;
        }
        
        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            //zde se staráme o pravidla typu ProtectedFolder a pouze ta, která jsou povolena
            var rules = Manager.Get<BackupRuleLoader>()
                               .Rules
                               .Where(rule => rule.RuleType == BackupRuleType.ProtectedFolder);

            //podívat se na každé pravidlo
            foreach(var rule in rules)
                handleRule(rule);
        }

        //v tomto slovníku uchováváme informace o struktuře složek
        private Dictionary<string, FolderNode> _folders = new Dictionary<string, FolderNode>();

        private void handleRule(BackupRule rule)
        {
            //zajímáme se o složky
            foreach(var src in rule.Sources.Directories)
            {
                //pokud jsme tuto složku již přidali do slovníku
                if (_folders.ContainsKey(src.path))
                {
                    //získáme dvě verze
                    // - první, starou, vezmem ze slovníku
                    // - druhou, novou, vytvoříme z toho, jak složka aktuálně vypadá
                    var old_node = _folders[src.path];
                    var new_node = FolderNode.FromPath(src.path);

                    //fChanging udává, zdali je nová složka jiná než stará složka
                    new_node.fChanging = !new_node.CompareAgainst(old_node);

                    //SMB_Log.Log($"old_node.fChanging == {old_node.fChanging}; new_node.fChangin == {new_node.fChanging}");

                    //pokud stará složka měla fChanging == true a nová má fChanging == false,
                    //znamená to, že změny ve složce se dokončily, a proto provedeme zálohu
                    //(také kontrolujeme, zdali ve složce vůbec jsou nějaká data (new_node.IsEmpty),
                    //záloha prázdné složky by byla asi ne úplně žádoucí)
                    if (old_node.fChanging == true && new_node.fChanging == false && !new_node.IsEmpty)
                    {
                        //pravidlo spustíme pouze pakliže je povoleno
                        //a zároveň jen jestli už jeho záloha neprobíhá,
                        //aby se to nemlátilo a nenastal bordel
                        if (rule.Enabled && !BackupTask.IsRuleExecuting(rule.LocalID))
                        {
                            SmbLog.Info($"FolderObserver: Zjištěna změna ve sledovaném adresáři {src.path}; spouštím pravidlo {rule.Name}", category: LogCategory.FolderObserver);
                            rule.GetBackupTaskRightNow().Execute();
                        }
                    }

                    //nahradíme info o staré složce infem o nové složce
                    _folders[src.path] = new_node;
                }
                //pokud jsme tuto složku ještě neviděli,
                else
                    //jednoduše přidáme info o ní do slovníku
                    _folders[src.path] = FolderNode.FromPath(src.path);
            }
        }

        public void Start()
        {
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
        }

        public void Clear()
        {
            _folders.Clear();
        }
    }
}
