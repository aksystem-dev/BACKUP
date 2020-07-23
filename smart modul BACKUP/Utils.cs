using Microsoft.Win32;
using SmartModulBackupClasses;
using SmartModulBackupClasses.Managers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration.Install;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace smart_modul_BACKUP
{
    public static class Utils
    {
        static ServiceState service => Manager.Get<ServiceState>();
        static InProgress inProgress => Manager.Get<InProgress>();

        public static string GetServiceInstalledPath()
        {
            var rk = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\smart modul BACKUP service");
            if (rk == null)
                return null;
            return (string)rk.GetValue("ImagePath");
        }

        /// <summary>
        /// Pokusí se nainstalovat službu. Pokud nedostane parametr cesta, zeptá se uživatele skrze openfiledialog.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool InstallService(string path = null, bool uninstall = false)
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
                path = path ?? dialog.FileName;
                if (!File.Exists(path))
                {
                    MessageBox.Show("Nenalezeno exe služby, nemohu jí nainstalovat.", "Chyba",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                try
                {
                    var assembly = Assembly.LoadFrom(path);
                    var installer = new AssemblyInstaller(assembly, null);
                    installer.UseNewContext = true;
                    var state = new Hashtable();
                    if (uninstall)
                        installer.Uninstall(state);
                    else
                        installer.Install(state);

                    return true;
                }
                catch (Exception e)
                {
                    if (MessageBox.Show(
                        $"{(uninstall ? "Odinstalace" : "Instalace")} služby se nezdařila\n\n{e.Message}\n\nZkusit znovu?",
                        "Chyba", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
                        continue;
                    else
                        return false;
                }
            }
        }

        //public static AssemblyInstaller GetServiceInstaller()
        //{
        //    var installer = new AssemblyInstaller(Assembly.LoadFrom()
        //}

        private static bool? _amIAdmin = null;


        /// <summary>
        /// Zdali jsme administrátoři
        /// </summary>
        /// <returns></returns>
        public static bool AmIAdmin()
        {
            if (_amIAdmin.HasValue)
                return _amIAdmin.Value;

            _amIAdmin = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
            return _amIAdmin.Value;
        }


        /// <summary>
        /// Nastaví, aby se GUI automaticky spouštělo (pomocí registru "SOFTWARE\Microsoft\Windows\CurrentVersion\Run")
        /// </summary>
        public static void SetAutoRun()
        {
            //nastavit registr tak, aby se GUI automaticky spouštělo po spuštění
            var rk = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            string exe = $"\"{Assembly.GetExecutingAssembly().Location}\" -hidden";
            if ((string)rk.GetValue("SMB GUI") != exe)
                rk.SetValue("SMB GUI", exe);
            rk.Close();
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

        /// <summary>
        /// Odešle do služby žádost o udělání zálohy dle daného pravidla
        /// </summary>
        /// <param name="Rule"></param>
        public static void DoSingleBackup(BackupRule Rule)
        {
            if (service.State == ServiceConnectionState.Connected)
            {
                //poslat žádost do služby
                //Utils.DoSingleBackup -> ServiceState.DoSingleBackup -> WCF volání
                var progress = service.DoSingleBackup(Rule);

                //dostali jsme objekt BackupInProgress, pomocí kterého můžeme sledovat průběh zálohy
                //služba nám bude posílat info o průběhu zálohy a bude se na daný BackupInProgress odkazovat pomocí id
                progress = inProgress.SetBackup(progress); 
                Rule.InProgress.Add(progress);

                //až to bude, chceme BackupInProgress po chvilce odstranit
                progress.Completed += async (obj, args) =>
                {
                    await Task.Delay(5000);
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
                Manager.Get<BackupRuleLoader>().Delete(rule.LocalID);
            }
        }

        /// <summary>
        /// Získá info o verzi.
        /// </summary>
        public static FileVersionInfo GetVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            return FileVersionInfo.GetVersionInfo(assembly.Location);
        }
    }
}
