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
            var l = FileUtils.ListDir(@"D:\SFTP server REBEX\data\SMB\00325-96594-25217-AAOEM\Backups\test1ku1\OneToOne", true);

            Console.WriteLine(string.Join("\n", l.Select(pair => $"{pair.Key} ({pair.Value.GetType().Name})")));

            Console.ReadKey();
        }
    }

    class ExampleType
    {
        public int ID = 0;

        public string strField = "test";
        public int intProp { get; set; } = 10;
        private string _strField = "private";

        public int unsettableProp { get; } = 10;

        public string strProp
        {
            get => "Hello world";
            set => Console.WriteLine(value);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"ID = {ID}");
            sb.AppendLine($"strField = {strField}");
            sb.AppendLine($"intProp = {intProp}");
            sb.AppendLine($"strProp = {strProp}");
            sb.AppendLine($"instance id: {this.GetHashCode()}");
            return sb.ToString();
        }
    }
}
