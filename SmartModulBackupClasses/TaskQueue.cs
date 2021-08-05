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
        readonly BlockingCollection<Func<Task>> tasks;
        CancellationTokenSource tokenSource;
        Thread loopThread = null;
        public bool Running => loopThread != null && loopThread.ThreadState == ThreadState.Running;

        public event EventHandler<Exception> OnExceptionCaught;

        public TaskQueue()
        {
            tasks = new BlockingCollection<Func<Task>>();
        }

        void start()
        {
            if (loopThread?.Join(1000) == false)
            {
                throw new InvalidOperationException("Už běžím!");
            }

            //Console.WriteLine("start");
            
            tokenSource = new CancellationTokenSource();
            loopThread = new Thread(() => loop().Wait()); // TODO: toto je fujky fujky...
            loopThread.Start();
        }

        void exit()
        {
            SmbLog.Info("TaskQueue exit called");

            tokenSource = null;
            loopThread = null;
        }
        

        async Task loop()
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
            }
            catch (Exception ex)
            {
                SmbLog.Error("error in TaskQueue", ex);
                OnExceptionCaught?.Invoke(this, ex);
            }
            finally
            {
                exit();
            }
        }

        public void Enqueue(Func<Task> task)
        {
            if (task == null)
            {
                SmbLog.Error("NULL passes to TaskQueue.Enqueue!!!");
                throw new ArgumentNullException(nameof(task));
            }

            if (!Running)
            {
                SmbLog.Info("TaskQueue thread not running. callin start...");
                start();
            }

            if (tasks == null)
            {
                SmbLog.Error("BlockingCollection for tasks is null for some reason");
                throw new NullReferenceException("tasks queue BlockingCollection is null");
            }
 
            tasks.Add(task);
        }

        public void WaitForAll()
        {
            loopThread.Join();
        }
    }
}
