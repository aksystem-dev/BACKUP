using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartModulBackupClasses
{
    public static class SMB_Utils
    {
        /// <summary>
        /// Vrátí id k identifikaci počítače. Aktuální implementace spočívá ve čtení registru, který obsahuje produkční id instalace Windows.
        /// </summary>
        /// <returns></returns>
        public static string GetComputerId()
            => Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion", "ProductId", null).ToString();

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
    }
}
