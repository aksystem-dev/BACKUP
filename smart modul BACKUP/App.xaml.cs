using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace smart_modul_BACKUP
{
    /// <summary>
    /// Interakční logika pro App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Argumenty aplikace. 
        /// </summary>
        public static string[] ARGS;

        /// <summary>
        /// Pokud je mezi argumenty "-force": 
        /// znamená to, že se má přeskočit logika, která vypne proces, pokud už na tomto uživateli stejný proces běží
        /// </summary>
        public static bool forceStart => ARGS.Contains("-force");

        /// <summary>
        /// Pokud je mezi argumenty "-autorun": 
        /// znamená to, že pokud služba není nainstalována nebo spuštěna,
        /// má se nainstalovat a spustit automaticky, aniž bychom se uživatele ptali, jestli si to tak přeje.
        /// </summary>
        public static bool autoRun => ARGS.Contains("-autorun");

        /// <summary>
        /// Pokud je mezi argumenty "-hidden":
        /// znamená to, že aplikace má začít skryta (čili schovaná v NotifyIcon)
        /// </summary>
        public static bool startHidden => ARGS.Contains("-hidden");

        private void OnAppStart(object sender, StartupEventArgs e)
        {
            ARGS = e.Args;
        }

        private void OnAppExit(object sender, ExitEventArgs e)
        {
            LoadedStatic.notifyIcon?.Dispose();
        }
    }
}
