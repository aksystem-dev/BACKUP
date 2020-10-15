using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartModulBackupClasses.Managers
{
    /// <summary>
    /// stará se o informace o počítačích na SFTP serveru
    /// </summary>
    public class SftpMetadataManager
    {
        /// <summary>
        /// vrátí informace o klientech využívajících tento server (do parametru již připojenou
        /// instanci SftpUploaderu)
        /// </summary>
        /// <param name="sftp"></param>
        /// <returns></returns>
        public static IEnumerable<PC_Info> GetPCInfos(SftpUploader sftp)
        {
            //seznam složek ve sdílené složce na serveru
            var dirs = sftp.ListDir(SMB_Utils.RemoteSharedDirectory, false, file => file.IsDirectory);

            foreach (var dir in dirs.Select(pair => pair.Value))
            {
                //cesta k složce s informacemi o zálohách
                var bkinfos_path = SMB_Utils.GetRemoteBkinfosPath(dir.FullName).NormalizePath();

                //neexistuje-li v této složce složka s informacemi o zálohách, nejedná se o složku počítače
                if (!sftp.client.Exists(bkinfos_path))
                    continue;

                //cesta k souboru s informacemi o PC
                var pcinfo_path = SMB_Utils.GetRemotePCinfoPath(dir.FullName).NormalizePath();

                PC_Info pcInfo = null;  //proměnná, kam uložíme informace o PC
                if (sftp.client.Exists(pcinfo_path))
                {
                    try { pcInfo = PC_Info.FromXML(sftp.client.ReadAllText(pcinfo_path)); }
                    catch (Exception ex)
                    {
                        SmbLog.Debug($"Nepodařilo se přečíst soubor xml s informacemi o PC\ncesta: {pcinfo_path}", ex, LogCategory.SFTP);
                    }
                }

                pcInfo = pcInfo ?? new PC_Info(); //pokud soubor neexistuje nebo se nepodařilo ho deserializovat, vytvořit novou instanci
                pcInfo.RemoteFolderName = dir.Name; //nastavit název složky
                yield return pcInfo; 
            }
        }

        /// <summary>
        /// vrátí informace o klientech využívajících tento server (používá se
        /// Manager.Get SftpUploader)
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<PC_Info> GetPCInfos()
        {
            using (var sftp = Manager.Get<SftpUploader>())
            {
                try
                {
                    sftp.Connect();
                    foreach (var pc in GetPCInfos(sftp))
                        yield return pc;
                }
                finally
                {
                    sftp.Disconnect();
                }
            }
        }

        /// <summary>
        /// nahraje informace o tomto PC na server (do parametru již připojenou instanci
        /// SftpUploaderu)
        /// </summary>
        public static void SetMyInfo(SftpUploader sftp)
        {
            var me = PC_Info.This;
            var meStr = me.ToXML();

            var dirPath = SMB_Utils.GetRemotePCDirectory().NormalizePath();
            var filePath = SMB_Utils.GetRemotePCinfoPath().NormalizePath();

            sftp.CreateDirectory(dirPath); //ujistit se, že existuje složka tohoto PC

            //vytvořit na serveru soubor, otevřít ho, napsat doň informace
            using (var writer = sftp.client.CreateText(filePath)) 
                writer.Write(meStr);
        }


        /// <summary>
        /// nahraje informace o tomto PC na server (používá se Manager.Get SftpUploader)
        /// </summary>
        public static void SetMyInfo()
        {
            using (var sftp = Manager.Get<SftpUploader>())
            {
                try
                {
                    sftp.Connect();
                    SetMyInfo(sftp);
                }
                finally
                {
                    sftp.Disconnect();
                }
            }
        }

    }
}
