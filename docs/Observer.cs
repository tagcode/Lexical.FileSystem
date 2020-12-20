using Lexical.FileSystem;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace docs
{
    // <doc>
    class Observer : IObserver<IEvent>
    {
        public Semaphore semaphore = new Semaphore(0, int.MaxValue);
        public IEvent Event;
        public bool Completed = false;
        public Exception Error;
        public void OnCompleted()
        {
            Completed = true;
            semaphore.Release(10);
        }
        public void OnError(Exception error)
        {
            Error = error;
            //semaphore.Release(10);
        }
        public void OnNext(IEvent value)
        {
            // Already captured event
            if (Event?.Observer != null) return;
            // Capture event
            this.Event = value;
            semaphore.Release(10);
        }
        public void Wait()
        {
            semaphore.WaitOne(10000);
        }
    }
    // </doc>
}
