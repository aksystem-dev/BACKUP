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
            var queue = new TaskQueue();
            for (int i = 0; i < 10; i++)
            {
                queue.Enqueue(GetTask());
            }

            Console.ReadKey();
        }

        static int ct = 0;
        static Func<Task> GetTask()
        {
            int val = ++ct;
            int zero = 0;
            return new Func<Task>(() =>
            {
                return Task.Run(() =>
                {
                    Task.Delay(1000).Wait();
                    Console.WriteLine(val.ToString() + " !");
                    int test = 5 / zero;
                });
            });
        }
    }
}
