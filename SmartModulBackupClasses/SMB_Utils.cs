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
        private static string pc_id = null;
        /// <summary>
        /// Vrátí id k identifikaci počítače. Aktuální implementace spočívá ve čtení registru, který obsahuje produkční id instalace Windows.
        /// </summary>
        /// <returns></returns>
        public static string GetComputerId()
        {
            if (pc_id == null)
            {
                var base_key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
                var cv_key = base_key.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion");
                pc_id = cv_key.GetValue("ProductId").ToString();
            } 
            return pc_id;
        }

        private static string pc_name = null;
        public static string GetComputerName()
        {
            if (pc_name == null)
                pc_name = Environment.MachineName;
            return pc_name;
        }

        /// <summary>
        /// Vrátí cestu na sftp serveru, kam se mají ukládat zálohy z tohoto pc
        /// </summary>
        /// <returns></returns>
        public static string GetRemoteBackupPath()
        {
            var pm = Manager.Get<AccountManager>();
            if (pm.State == LoginState.Offline)
                return Path.Combine(Manager.Get<ConfigManager>().Config.SFTP.Directory, GetComputerId(), "Backups");
            else
                return Path.Combine(pm.SftpInfo.Directory, GetComputerId(), "Backups");
        }

        /// <summary>
        /// Vrátí cestu na sftp serveru, kam se mají ukládat info o zálohách z tohoto pc
        /// </summary>
        /// <returns></returns>
        public static string GetRemoteBkinfosPath()
        {
            var pm = Manager.Get<AccountManager>();
            if (pm.State == LoginState.Offline)
                return Path.Combine(Manager.Get<ConfigManager>().Config.SFTP.Directory, GetComputerId(), "bkinfos");
            else
                return Path.Combine(pm.SftpInfo.Directory, GetComputerId(), "bkinfos");
        }

        /// <summary>
        /// Vrátí název souboru
        /// </summary>
        /// <param name="bk"></param>
        /// <returns></returns>
        public static string BkInfoNameStr(this Backup bk)
            => bk.RefRuleName + "_" + bk.EndDateTime.ToString("dd-MM-yyyy") + "_" + bk.LocalID + ".xml";

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
}
