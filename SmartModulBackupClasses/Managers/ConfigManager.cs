using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartModulBackupClasses.Managers
{
    public class ConfigManager : INotifyPropertyChanged
    {
        private Config _config = null;

        public event PropertyChangedEventHandler PropertyChanged;

        public bool LazyLoad { get; set; } = true;

        public ConfigManager() { }

        public Config Config
        {
            get
            {
                if (_config == null)
                {
                    if (LazyLoad)
                        Load();
                    else
                        throw new InvalidOperationException("Config ještě nebyl načten.");
                }
                return _config;
            }
            set
            {
                _config = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Config)));
            }
        }

        public ConfigManager Load()
        {
            if (File.Exists(Const.CFG_FILE))
                _config = Config.FromXML(File.ReadAllText(Const.CFG_FILE));
            else
                _config = new Config();
            return this;
        }

        public void Save()
        {
            var dir = Path.GetDirectoryName(Const.CFG_FILE);
            if (dir != "" && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(Const.CFG_FILE, _config.ToXML());
        }
    }
}
