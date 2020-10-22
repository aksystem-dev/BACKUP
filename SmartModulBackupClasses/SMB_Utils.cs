using Microsoft.Win32;
using Renci.SshNet;
using SmartModulBackupClasses.Managers;
using SmartModulBackupClasses.WebApi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SmartModulBackupClasses
{
    public static class SMB_Utils
    {
        private static readonly Dictionary<ClientIdType, string> _ids
            = new Dictionary<ClientIdType, string>();

        public const ClientIdType ID_TYPE_TO_USE = ClientIdType.ComputerName;

        private static string pc_key = null;

        /// <summary>
        /// Vrátí aktivační klíč aktuální instalace Windows.
        /// </summary>
        /// <returns></returns>
        public static string GetWindowsKey()
        {
            if (pc_key == null)
            {
                var base_key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
                var cv_key = base_key.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion");
                pc_key = cv_key.GetValue("ProductId").ToString();
            }

            return pc_key;
        }

        /// <summary>
        /// vrátí id tohoto počítače, které by se mělo používat
        /// </summary>
        /// <returns></returns>
        public static string GetComputerId()
            => GetComputerId(ID_TYPE_TO_USE);

        /// <summary>
        /// vrátí id tohoto počítače daného typu
        /// </summary>
        /// <param name="idType"></param>
        /// <returns></returns>
        public static string GetComputerId(ClientIdType idType)
        {
            if (_ids.ContainsKey(idType))
                return _ids[idType];

            switch (idType)
            {
                case ClientIdType.ComputerName:
                    return _ids[idType] = GetComputerName();
                case ClientIdType.WindowsKey:
                    return _ids[idType] = GetWindowsKey();
                default:
                    throw new NotImplementedException();
            }
        }

        private static string pc_name = null;
        /// <summary>
        /// Vrátí název tohoto PC
        /// </summary>
        /// <returns></returns>
        public static string GetComputerName()
        {
            if (pc_name == null)
                pc_name = Environment.MachineName;
            return pc_name;
        }

        /// <summary>
        /// Vrátí kořenovou složku na SFTP, kam všechny počítače ukládají svá data.
        /// </summary>
        /// <returns></returns>
        public static string RemoteSharedDirectory 
        {
            get
            {
                var pm = Manager.Get<AccountManager>();
                if (pm.State == LoginState.Offline)
                    return Manager.Get<ConfigManager>().Config.SFTP.Directory;
                else
                    return pm.SftpInfo.Directory;
            }
        }

        /// <summary>
        /// Cesta ke vzdálené složce patřící TOMUTO PC
        /// </summary>
        /// <returns></returns>
        public static string GetRemotePCDirectory()
        {
            return Path.Combine(RemoteSharedDirectory, GetComputerId());
        }

        /// <summary>
        /// vrátí cestu ke vzdálené složce patřící pc s DANÝM názvem složky
        /// </summary>
        /// <param name="pcFolder"></param>
        /// <returns></returns>
        public static string GetRemotePCDirectory(string pcFolder)
        {
            return Path.Combine(RemoteSharedDirectory, pcFolder);
        }


        /// <summary>
        /// Vrátí cestu na sftp serveru, kam se mají ukládat zálohy z TOHOTO pc
        /// </summary>
        /// <returns></returns>
        public static string GetRemoteBackupPath()
        {
            return Path.Combine(GetRemotePCDirectory(), Const.REMOTE_DIR_BACKUPS);
        }

        /// <summary>
        /// Vrátí cestu na sftp serveru, kam se mají ukládat zálohy z DANÉHO pc
        /// </summary>
        /// <returns></returns>
        public static string GetRemoteBackupPath(string pcFolder)
        {
            return Path.Combine(GetRemotePCDirectory(pcFolder), Const.REMOTE_DIR_BACKUPS);
        }

        /// <summary>
        /// Vrátí cestu na sftp serveru, kam se mají ukládat info o zálohách z TOHOTO pc
        /// </summary>
        /// <returns></returns>
        public static string GetRemoteBkinfosPath()
        {
            return Path.Combine(GetRemotePCDirectory(), Const.BK_INFOS_FOLDER);
        }

        /// <summary>
        /// Vrátí cestu na sftp serveru, kam se mají ukládat info o zálohách z DANÉHO pc
        /// </summary>
        /// <returns></returns>
        public static string GetRemoteBkinfosPath(string pcFolder)
        {
            return Path.Combine(GetRemotePCDirectory(pcFolder), Const.BK_INFOS_FOLDER);
        }

        /// <summary>
        /// vrátí vzdálenou cestu k souboru s informacemi o TOMTO klientovi
        /// </summary>
        /// <returns></returns>
        public static string GetRemotePCinfoPath()
        {
            return Path.Combine(GetRemotePCDirectory(), Const.REMOTE_PC_INFO);
        }

        /// <summary>
        /// vrátí vzdálenou cestu k souboru s informacemi o DANÉM klientovi
        /// </summary>
        /// <param name="pcFolder"></param>
        /// <returns></returns>
        public static string GetRemotePCinfoPath(string pcFolder)
        {
            return Path.Combine(GetRemotePCDirectory(pcFolder), Const.REMOTE_PC_INFO);
        }



        public static string PropertiesString(this object obj)
        {
            StringBuilder str = new StringBuilder();

            var type = obj.GetType();
            str.AppendLine(type.Name);
            foreach(var prop in type.GetProperties())
                str.AppendLine($"  - Property {prop.Name}: {(prop.GetValue(obj)?.ToString() ?? "null")}");
            foreach (var field in type.GetFields())
                str.AppendLine($"  - Field {field.Name}: {(field.GetValue(obj)?.ToString() ?? "null")}");

            return str.ToString();
        }

        public static float Lerp(float from, float to, float coeff, bool clamp = true)
        {
            if (clamp)
            {
                if (coeff > 1) coeff = 1;
                else if (coeff < 0) coeff = 0;
            }

            return from + (to - from) * coeff;
        }

        /// <summary>
        /// Spustí task a počká až se dokončí.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="task"></param>
        /// <returns></returns>
        public static T Sync<T>(Func<Task<T>> taskf)
        {
            try
            {
                var task = Task.Run(() => taskf());
                task.Wait();
                return TaskResult(task);
            }
            catch (Exception ex)
            {
                throw UnpackException(ex);
            }
        }

        /// <summary>
        /// Spustí task synchronně.
        /// </summary>
        /// <param name="task"></param>
        public static void Sync(Func<Task> taskf)
        {
            try
            {
                var task = Task.Run(() => taskf());
                task.Wait();
                TaskResult(task);
            }
            catch (Exception ex)
            {
                throw UnpackException(ex);
            }
        }

        /// <summary>
        /// Vyhodnotí dokončený task a vyhodí případnou výjimku.
        /// </summary>
        /// <param name="task"></param>
        public static void TaskResult(Task task)
        {
            switch (task.Status)
            {
                case TaskStatus.RanToCompletion:
                    return;
                case TaskStatus.Faulted:
                    throw UnpackException(task.Exception);
                case TaskStatus.Canceled:
                    throw UnpackException(task.Exception) ?? new TimeoutException();
                default:
                    throw new NotImplementedException("i cannot do with the " + task.Status.ToString() + " too complex pls help");
            }
        }

        /// <summary>
        /// Vyhodnotí dokončený task a vyhodí případnou výjimku.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="task"></param>
        /// <returns></returns>
        public static T TaskResult<T>(Task<T> task)
        {
            switch (task.Status)
            {
                case TaskStatus.RanToCompletion:
                    return task.Result;
                case TaskStatus.Faulted:
                    throw UnpackException(task.Exception);
                case TaskStatus.Canceled:
                    throw UnpackException(task.Exception) ?? new TimeoutException();
                default:
                    throw new NotImplementedException("i cannot do with the " + task.Status.ToString() + " too complex pls help");
            }
        }

        /// <summary>
        /// Dá tasku nějaký čas na dokončení, pokud to nestihne, vyhodí se výjimka
        /// </summary>
        /// <param name="task"></param>
        /// <param name="ms"></param>
        /// <returns></returns>
        public static Task Timeout(Task task, int ms)
        {
            return Task.Run(() =>
            {
                if (task.Status == TaskStatus.Created)
                    task.Start();
                if (task.Wait(ms))
                    return;
                else
                    throw new TimeoutException();
            });
        }

        /// <summary>
        /// Dá tasku nějaký čas na dokončení, pokud to nestihne, vyhodí se výjimka
        /// </summary>
        /// <param name="task"></param>
        /// <param name="ms"></param>
        /// <returns></returns>
        public static Task<T> Timeout<T>(Task<T> task, int ms)
        {
            return Task.Run(() =>
            {
                if (task.Status == TaskStatus.Created)
                    task.Start();
                if (task.Wait(ms))
                    return TaskResult(task);
                else
                    throw new TimeoutException();
            });
        }

        public static Exception UnpackException(Exception ex)
        {
            if (ex == null)
                return null;

            if (ex is AggregateException aex)
            {
                var flattened = aex.Flatten();

                int ct = 0;
                var en = aex.InnerExceptions.GetEnumerator();
                Exception curr = null;

                while(en.MoveNext())
                {
                    ct++;
                    curr = en.Current;
                    if (ct >= 2)
                        return ex;
                }

                return curr;
            }

            return ex;
        }

        /// <summary>
        /// Vrátí instanci SftpResponse pro připojení na SFTP. Vrátí null, pokud údaje nejsou dostupné.
        /// </summary>
        /// <returns></returns>
        public static SftpResponse GetSftpConnection()
        {
            var acc = Manager.Get<AccountManager>();
            if (acc?.State == LoginState.LoginSuccessful && acc.SftpInfo != null)
                return acc.SftpInfo;
            else if (acc?.State == LoginState.LoginFailed)
                return null;
            else
            {
                var cfg = Manager.Get<ConfigManager>()?.Config?.SFTP;

                if (cfg == null)
                    return null;

                return new SftpResponse()
                {
                    Directory = cfg.Directory,
                    Host = cfg.Host,
                    Password = cfg.Password.Value,
                    Port = cfg.Port,
                    Username = cfg.Username
                };
            }
        }

        /// <summary>
        /// Vrátí hash aktuálního připojení na SFTP, aby si mohly zálohy pamatovat, na jaký SFTP server byly nahrány.
        /// </summary>
        /// <returns></returns>
        public static string GetSftpHash()
        {
            var info = GetSftpConnection();

            if (info == null) return null;

            var builder = new StringBuilder();
            
            builder.AppendLine(info.Host);
            builder.AppendLine(info.Username);
            builder.AppendLine(info.Directory);

            return HashString(builder.ToString());
        }

        /// <summary>
        /// Vrátí hash daného řetězce ve formátu Base 64 String.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string HashString(string str)
        {
            var bytesToHash = Encoding.UTF8.GetBytes(str);

            using(var hasher = SHA256.Create())
            {
                var hashedBytes = hasher.ComputeHash(bytesToHash);
                return Convert.ToBase64String(hashedBytes);
            }
        }

        /// <summary>
        /// Vrátí ID aktuálního plánu. Pokud plán není, vrátí -1.
        /// </summary>
        /// <returns></returns>
        public static int GetCurrentPlanId()
        {
            var acc = Manager.Get<AccountManager>()?.HelloInfo?.ActivePlan;

            if (acc == null)
                return -1;

            return acc.ID;
        }

    }

    public enum ClientIdType
    {
        /// <summary>
        /// Počítač je identifikován pomocí Windows aktivačního klíče
        /// </summary>
        WindowsKey,

        /// <summary>
        /// Počítač je identifikován pomocí názvu počítače
        /// </summary>
        ComputerName
    }
}
