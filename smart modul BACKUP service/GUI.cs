using SmartModulBackupClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smart_modul_BACKUP_service
{
    /// <summary>
    /// Poskytuje metody pro komunikaci s uživatelským rozhraním
    /// </summary>
    public class GUI
    {
        private WCF.SmartModulBackupInterface wcf_interface;

        private static void logInfo(string message)
        {
            SmbLog.Info(message, null, LogCategory.ServiceHost);
        }

        private static void logError(string error, Exception ex = null)
        {
            SmbLog.Error(error, null, LogCategory.ServiceHost);
        }

        private static void logWarn(string message)
        {
            SmbLog.Warn(message, null, LogCategory.ServiceHost);
        }

        public GUI(WCF.SmartModulBackupInterface wcf)
        {
            wcf_interface = wcf;
        }

        public bool Connected => wcf_interface.connected;

        private void TryCallback(Action method)
        {
            Task.Run(() =>
            {
                if (wcf_interface.Callback == null)
                {
                    logInfo($"GUI nepřipojeno, nemůžu mu předat zprávu.");
                }

                try
                {
                    method();
                }
                catch (TimeoutException e)
                {
                    logWarn("Čas vypršel. Odpojuji rozhraní");
                    wcf_interface.context.Abort();
                }
                catch (Exception e)
                {
                    logError("Došlo k chybě při volání metody na GUI.", e);

                    try
                    {
                        wcf_interface.context.Abort();
                    }
                    catch (Exception ee)
                    {
                        logError("Došlo k chybě při rušení spojení s GUI", ee);
                    }
                }
            });
        }

        public void ShowError(string error)
            => TryCallback(() => wcf_interface.Callback.ShowError(error));

        public void TestConnection()
            => TryCallback(() => wcf_interface.Callback.TestConnection());
    }
}
