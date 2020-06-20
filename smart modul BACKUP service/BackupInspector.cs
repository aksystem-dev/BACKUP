using SmartModulBackupClasses;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace smart_modul_BACKUP_service
{
    /// <summary>
    /// Kontroluje zálohy.
    /// </summary>
    //public class BackupInspector
    //{
    //    private Timer _timer;
    //    public double? Interval => _timer?.Interval;

    //    /// <summary>
    //    /// Jestli je inspektor momentálně aktivní (pravidelně kontroluje zálohy)
    //    /// </summary>
    //    public bool Running => _timer?.Enabled == true;

    //    /// <summary>
    //    /// Začne pravidelně kontrolovat zálohy v daném intervalu.
    //    /// </summary>
    //    /// <param name="interval"></param>
    //    public void Start(int interval)
    //    {
    //        if (_timer == null)
    //        {
    //            _timer = new Timer(interval);
    //            _timer.Elapsed += _timer_Elapsed;
    //        }
    //        else
    //        {
    //            if (_timer.Enabled)
    //                _timer.Stop();
    //            _timer.Interval = interval;
    //        }

    //        _timer.Start();
    //    }

    //    /// <summary>
    //    /// Přestane pravidelně kontrolovat zálohy v daném intervalu.
    //    /// </summary>
    //    public void Stop()
    //    {
    //        _timer?.Stop();
    //    }

    //    private void _timer_Elapsed(object sender, ElapsedEventArgs e)
    //    {
    //        InspectAndFix();
    //    }

    //    /// <summary>
    //    /// Zkontroluje zálohy a opraví je.
    //    /// </summary>
    //    public void InspectAndFix()
    //    {
    //        var backups = Utils.SavedBackups.GetInfos();

    //        SftpUploader sftp = null;
    //        if (backups.Any(f => f.AvailableRemotely))
    //        {
    //            sftp = Utils.SftpFactory.GetInstance();

    //            try
    //            {
    //                sftp.Connect();
    //            }
    //            catch (Exception ex)
    //            {
    //                sftp = null;
    //            }
    //        }

    //        //projdeme všechny zálohy
    //        foreach (var backup in backups)
    //        {
    //            //pokud nám záloha tvrdí, že je dostupná na tomto počítači
    //            if (backup.AvailableOnThisComputer)
    //            {
    //                //zkontrolujem si to, jestli nám lže, tak jí to vysvětlíme
    //                if (!File.Exists(backup.LocalPath))
    //                    backup.AvailableLocally = false;
    //            }

    //            //pokud nám záloha tvrdí, že je dostupná na serveru (a zárověň máme připojení na server)
    //            if (sftp != null && backup.AvailableRemotely)
    //            {
    //                //ověříme to, kdyžtak jí to vysvětlíme
    //                if (!sftp.client.Exists(backup.RemotePath))
    //                    backup.AvailableRemotely = false;
    //            }
    //        }

    //        Utils.SavedBackups.SaveInfos();

    //        sftp?.Disconnect();
    //        sftp?.client?.Dispose();
    //    }
    //}
}
