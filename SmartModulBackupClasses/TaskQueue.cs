using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SmartModulBackupClasses
{
    /// <summary>
    /// Spouští tasky jeden po druhém na vedlejším vlákně
    /// </summary>
    public class TaskQueue
    {
        BlockingCollection<Func<Task>> tasks;
        CancellationTokenSource tokenSource;
        Thread loopThread = null;
        public bool Running => loopThread != null && loopThread.ThreadState == ThreadState.Running;

        public event EventHandler<Exception> OnExceptionCaught;

        public TaskQueue()
        {
        }

        void start()
        {
            if (loopThread?.Join(1000) == false)
                throw new InvalidOperationException("Už běžím!");

            //Console.WriteLine("start");
            tasks = new BlockingCollection<Func<Task>>();
            tokenSource = new CancellationTokenSource();
            loopThread = new Thread(loop);
            loopThread.Start();
        }

        void exit()
        {
            tasks = null;
            tokenSource = null;
            loopThread = null;
        }
        

        async void loop()
        {
            try
            {
                while (!tokenSource.IsCancellationRequested && tasks.Count > 0)
                {
                    var task = tasks.Take(tokenSource.Token);

                    try
                    {
                        await task();
                    }
                    catch (Exception ex)
                    {
                        OnExceptionCaught?.Invoke(this, ex);
                    }
                }

                exit();
            }
            catch (Exception ex)
            {

            }
        }

        public void Enqueue(Func<Task> task)
        {
            if (!Running)
                start();
            tasks.Add(task);
        }

        public void WaitForAll()
        {
            loopThread.Join();
        }
    }
}
