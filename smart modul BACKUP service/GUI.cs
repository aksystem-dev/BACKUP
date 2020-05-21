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
                    Logger.Log($"GUI nepřipojeno, nemůžu mu předat zprávu.");
                }

                try
                {
                    method();
                }
                catch (TimeoutException e)
                {
                    Logger.Error($"Chyba při komunikaci s GUI ({e.GetType().Name}) \n\n {e.Message}");
                    Logger.Warn("Odpojuji rozhraní");
                    wcf_interface.context.Abort();
                }
                catch (Exception e)
                {
                    Logger.Error($"Chyba při komunikaci s GUI ({e.GetType().Name}) \n\n {e.Message}");
                    try
                    {
                        wcf_interface.context.Abort();
                    }
                    catch (Exception ee)
                    {
                        Logger.Error($"...{ee.GetType().Name}\n\n{ee.Message}");
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
