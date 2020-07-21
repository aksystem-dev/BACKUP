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

        /// <summary>
        /// Jestli automaticky načíst Config při čtení, pokud ještě načten nebyl.
        /// </summary>
        public bool LazyLoad { get; set; } = true;

        /// <summary>
        /// Zdali existuje soubor s konfigurací.
        /// </summary>
        public bool ConfigFileExists => File.Exists(Const.CFG_FILE);

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

        /// <summary>
        /// Načíst config
        /// </summary>
        /// <returns></returns>
        public ConfigManager Load()
        {
            if (File.Exists(Const.CFG_FILE))
                _config = Config.FromXML(File.ReadAllText(Const.CFG_FILE));
            else
                _config = new Config();
            _config.Loaded();
            return this;
        }

        /// <summary>
        /// Uložit config
        /// </summary>
        public void Save()
        {
            var dir = Path.GetDirectoryName(Const.CFG_FILE);
            if (dir != "" && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(Const.CFG_FILE, _config.ToXML());
        }
    }
}
