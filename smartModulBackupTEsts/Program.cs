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
            var c = new Conditions()
            {
                Time = "10:00 - 23:00 / 00:10"
            };

            Console.WriteLine(String.Join(", ", c.AvailableDateTimesInTimeSpan(
                DateTime.Parse("18.4.2020 19:00"),
                DateTime.Parse("18.4.2020 20:00"),
                exclusiveStart: true,
                exclusiveEnd: true
                )));

            Console.WriteLine(String.Join(", ", c.AvailableDateTimesInTimeSpan(
                DateTime.Parse("18.4.2020 20:00"),
                DateTime.Parse("18.4.2020 21:00"),
                exclusiveStart: true,
                exclusiveEnd: true
                )));

            Console.ReadLine();
        }
    }
}
