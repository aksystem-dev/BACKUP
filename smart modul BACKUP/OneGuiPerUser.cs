using SmartModulBackupClasses;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace smart_modul_BACKUP
{
    /// <summary>
    /// Stará se o to, aby na jednom uživateli běžela maximálně jedna instance uživatelského rozhraní
    /// </summary>
    public class OneGuiPerUser : IDisposable
    {
        private NamedPipeServerStream listener;
        private StreamReader reader;
        public bool Server { get; private set; } = false;
        public string PipeName => "SMB_GUI_" + WindowsIdentity.GetCurrent().Name.Replace('\\','_');

        public event Action OpenSignalReceived;

        public void Dispose()
        {
            reader?.Dispose();
        }

        /// <summary>
        /// Zjistí, jestli na tomto účtu už běží tento program. Jestli ano, řekne mu, aby se dal do popředí, a vrátí false.
        /// Pokud vrátí false, mělo by se toto okno zavřít.
        /// </summary>
        /// <returns></returns>
        public bool Init()
        {
            SmbLog.Info("OneGuiPerUser init", null, LogCategory.OneGuiPerUser);

            //vytvoříme trubku s daným názvem a pokusíme se připojit jako klient
            //pokud je připojení úspěšné, znamená to, že už jeden tento proces běží

            SmbLog.Info($"Creating named pipe with name {PipeName}", null, LogCategory.OneGuiPerUser);

            var client = new NamedPipeClientStream(PipeName);
            try
            {
                client.Connect(1000);
                if (client.IsConnected)
                {
                    SmbLog.Info($"Connected to an already running instance hosting a pipe with name {PipeName}", null, LogCategory.OneGuiPerUser);

                    //pokud už instance tohoto programu běží, řekneme jí, ať se dá do popředí
                    using (var writer = new StreamWriter(client))
                        writer.WriteLine("OPEN");

                    Thread.Sleep(500);

                    SmbLog.Info("OneGuiPerUser Init() returning", null, LogCategory.OneGuiPerUser);

                    //vrátíme false
                    return false;
                }
            }
            catch (TimeoutException) { }
            catch (IOException) { }

            //pokud připojení nebylo úspěšné, zbavíme se klienta a stane se z nás server
            client.Dispose();

            SmbLog.Info("An already running instance not found; I will become the pipe host", null, LogCategory.OneGuiPerUser);

            Server = true;
            var access = new PipeSecurity();
            access.SetAccessRule(new PipeAccessRule(
                new SecurityIdentifier(WellKnownSidType.WorldSid, null),
                PipeAccessRights.FullControl,
                System.Security.AccessControl.AccessControlType.Allow
                ));
            listener = new NamedPipeServerStream(PipeName, PipeDirection.InOut, 10, PipeTransmissionMode.Byte, PipeOptions.Asynchronous,
                1024, 1024, access);

            SmbLog.Info($"Pipe with name {PipeName} opened", null, LogCategory.OneGuiPerUser);

            listener.BeginWaitForConnection(ConnectionReceived, null);

            return true;
        }

        //když se připojí nová instance gui, řekne nám, abychom se otevřeli
        private void ConnectionReceived(IAsyncResult result)
        {
            SmbLog.Info($"Connection received on {PipeName}", null, LogCategory.OneGuiPerUser);
            listener.EndWaitForConnection(result);
           
            reader = new StreamReader(listener);
            if (reader.ReadLine() == "OPEN")
                OpenSignalReceived?.Invoke();

            listener.Disconnect();
            listener.BeginWaitForConnection(ConnectionReceived, null);
        }


    }
}
