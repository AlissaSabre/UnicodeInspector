using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace UnicodeInspector
{
    public abstract class IncrementalUpdater<T> : IDisposable where T: class
    {
        public IncrementalUpdater()
        {
            Worker = new Thread(WorkerLoop);
            Worker.IsBackground = true;
        }

        private readonly Thread Worker;

        private readonly object Lock = new Object();

        private T Input = null;

        private volatile bool Cancelled = false;

        private volatile bool Disposed = false;

        public virtual void Start()
        {
            Worker.Start();
        }

        public virtual bool IsRunning { get { return Worker.IsAlive; } }

        public virtual void Update(T data)
        {
            lock (Lock)
            {
                if (Disposed) throw new InvalidOperationException("Already disposed.");
                Input = data;
                Cancelled = true;
                Monitor.Pulse(Lock);
            }
        }

        public virtual void Cancel()
        {
            lock (Lock)
            {
                Input = null;
                Cancelled = true;
                // No pulse here, since waiting Worker can keep waiting.
            }
        }

        public virtual void Dispose()
        {
            lock (Lock)
            {
                Input = null;
                Cancelled = true;
                Disposed = true;
                Monitor.PulseAll(Lock);
            }
        }

        protected virtual void WorkerLoop()
        {
            for (;;)
            {
                T input;
                lock (Lock)
                {
                    for (;;)
                    {
                        if (Disposed) return;
                        input = Input;
                        if (input != null) break;
                        Monitor.Wait(Lock);
                    }
                    Input = null;
                    Cancelled = false;
                }

                DoWork(input);
            }
        }

        protected bool IsCancelled()
        {
            return Cancelled;
        }

        protected abstract void DoWork(T input);
    }
}
