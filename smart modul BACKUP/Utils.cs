using SmartModulBackupClasses;
using System;
using System.Collections.Generic;
using System.Configuration.Install;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace smart_modul_BACKUP
{
    public static class Utils
    {
        /// <summary>
        /// Pokusí se nainstalovat službu. Pokud nedostane parametr cesta, zeptá se uživatele skrze openfiledialog.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool InstallService(string path = null)
        {
            var dialog = path == null ? new System.Windows.Forms.OpenFileDialog()
            {
                Title = "Vyberte exe",
                Filter = ".exe soubory | *.exe",
                InitialDirectory = AppDomain.CurrentDomain.BaseDirectory
            } : null;

            while (true)
            {
                if (path == null && dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    return false;

                try
                {
                    ManagedInstallerClass.InstallHelper(new string[] { dialog.FileName });
                    return true;
                }
                catch (Exception e)
                {
                    if (MessageBox.Show(
                        $"Instalace služby se nezdařila\n\n{e.Message}\n\nZkusit znovu?",
                        "Chyba", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
                        continue;
                    else
                        return false;
                }
            }
        }

        /// <summary>
        /// Zdali jsme administrátoři
        /// </summary>
        /// <returns></returns>
        public static bool AmIAdmin()
        {
            return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
        }

        /// <summary>
        /// Shodí tento proces a začne ho znovu jako administrátor.
        /// </summary>
        /// <param name="args"></param>
        public static void RestartAsAdmin(string[] args)
        {
            var processStartInfo = new ProcessStartInfo()
            {
                Arguments = String.Join(" ", args),
                FileName = Assembly.GetEntryAssembly().Location,
                Verb = "runas"
            };

            Process.Start(processStartInfo);

            Environment.Exit(0);
        }

        public static IEnumerable<T> MultiUnion<T>(params IEnumerable<T>[] enumerables)
        {
            IEnumerable<T> to_return = Enumerable.Empty<T>();
            foreach (var e in enumerables)
                to_return = to_return.Union(e);
            return to_return;
        }

        public static void DoSingleBackup(BackupRule Rule)
        {
            if (LoadedStatic.service.State == ServiceConnectionState.Connected)
            {
                var progress = LoadedStatic.service.DoSingleBackup(Rule);
                progress = LoadedStatic.InProgress.SetBackup(progress);
                Rule.InProgress.Add(progress);
                progress.Completed += async (obj, args) =>
                {
                    await Task.Delay(2000);
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        Rule.InProgress.Remove(progress);
                    });
                };
            }
            else
                MessageBox.Show("Služba není připojena, nelze provést jednorázovou zálohu.");
        }

        public static void DeleteRule(BackupRule rule, bool messageBox = true)
        {
            if (!messageBox || MessageBox.Show($"Opravdu chcete odstranit pravidlo {rule.Name}?", "Skutečně?",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                //jedná-li se o poslední pravidlo, chceme uložit jeho ID 
                if (LoadedStatic.rules.Count == 1)
                    File.WriteAllText("ruleid", rule.LocalID.ToString());

                File.Delete(rule.path);
                LoadedStatic.rules.Remove(rule);
            }
        }
    }
}
