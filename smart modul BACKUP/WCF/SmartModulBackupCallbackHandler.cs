using smart_modul_BACKUP.ServiceInterface;

using SmartModulBackupClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace smart_modul_BACKUP.WCF
{
    /// <summary>
    /// Zde se zpracovávají zprávy ze služby.
    /// </summary>
    [CallbackBehavior(ConcurrencyMode = ConcurrencyMode.Reentrant, UseSynchronizationContext = false)]
    class SmartModulBackupCallbackHandler : ISmartModulBackupInterfaceCallback
    {
        public event Action OnServiceDisconnected;
        public SmartModulBackupInterfaceClient client;

        public void BackupStarted(string ruleName)
        {
            LoadedStatic.notifyIcon.ShowBalloonTip(2000, "Nastává záloha", $"Spouští se zálohovací pravidlo {ruleName}", ToolTipIcon.Info);

            //if (rule != null)
            //    LoadedStatic.notifyIcon.ShowBalloonTip(2000, "Nastává záloha", $"Spouští se zálohovací pravidlo {rule.Name}", ToolTipIcon.Info);
            //else
            //    LoadedStatic.notifyIcon.ShowBalloonTip(2000, "Nastává záloha", $"Spouští se zálohovací pravidlo s id {ruleName}. To je zvláštní, protože tento id neznám.", ToolTipIcon.Warning);
        }

        public void TestConnection()
        {
            client.ImStillHere();
            //return true;
        }

        public void BackupEnded(string ruleName, bool success)
        {
            //ukázat bublinu
            if (success)
                LoadedStatic.notifyIcon?.ShowBalloonTip(2000, "Záloha dokončena", $"Úspěšně dokončeny zálohy dle pravidla {ruleName}", ToolTipIcon.Info);
            else
                LoadedStatic.notifyIcon?.ShowBalloonTip(2000, "Záloha dokončena s chybami", $"{ruleName} uplatněno, ale došlo k chybám", ToolTipIcon.Warning);

            //znovu načíst uložené zálohy, protože nové pravidlo pravděpodobně přidalo záznam
            System.Windows.Application.Current.Dispatcher.Invoke(LoadedStatic.LoadSavedBackups);
        }

        public void ShowError(string error)
        {
            LoadedStatic.notifyIcon?.ShowBalloonTip(2000, "Chyba", error, ToolTipIcon.Error);
        }

        public void ShowMsg(string msg)
        {
            //MessageBox.Show(msg);
        }

        public void RestoreComplete(RestoreResponse response)
        {
            //LoadedStatic.service.RestoreCompleteBubble(response);
        }

        public void Goodbye()
        {
            OnServiceDisconnected?.Invoke();
        }
    }
}
