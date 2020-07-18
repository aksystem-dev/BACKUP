using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartModulBackupClasses;

namespace smartModulBackupTEsts
{
    class Program
    {
        static void Main(string[] args)
        {
            var sftp = new SftpUploader("192.168.0.171", 22, "tester", "password");
            sftp.Connect();

            
            Console.WriteLine(sftp.GetDirSize("Test"));

            sftp.Disconnect();

            Console.ReadKey();
        }
    }
}
