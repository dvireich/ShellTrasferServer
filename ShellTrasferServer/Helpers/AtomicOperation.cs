using PostSharp.Extensibility;
using PostSharp.Patterns.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Data
{
    [Log(AttributeExclude = true)]
    public class AtomicOperation : IAtomicOperation
    {
        private object AtomicOperationLock = new Object();
        private object AtomicSubscribeLock = new Object();
        private object AtomicDownloadLock = new Object();
        private object AtomicUploadLock = new Object();

        public bool IsTransferingData { get; set; }

        private T PerformAsAtomicFunction<T>(Func<T> func, object lockobj, bool safeToPassLock = false)
        {
            if (!safeToPassLock)
                Monitor.Enter(lockobj);
            T ret = default(T);
            try
            {
                ret = func();
            }
            catch { }
            if (!safeToPassLock)
                Monitor.Exit(lockobj);
            return ret;
        }

        public T PerformAsAtomicFunction<T>(Func<T> func, bool safeToPassLock = false)
        {
            return PerformAsAtomicFunction<T>(func, AtomicOperationLock);
        }

        public void PerformAsAtomicFunction(Action func, bool safeToPassLock = false)
        {
            if (!safeToPassLock)
                Monitor.Enter(AtomicOperationLock);
            try
            {
                func();
            }
            catch { }
            if (!safeToPassLock)
                Monitor.Exit(AtomicOperationLock);
        }

        public void PerformAsAtomicDownload(Action func)
        {

            Monitor.Enter(AtomicDownloadLock);
            try
            {
                func();
            }
            catch { }
            Monitor.Exit(AtomicDownloadLock);
            IsTransferingData = false;

        }

        public T PerformAsAtomicUpload<T>(Func<T> func)
        {
            return PerformAsAtomicFunction<T>(func, AtomicUploadLock);
        }

        public bool PerformAsAtomicSubscribe(Func<bool> func)
        {
            return PerformAsAtomicFunction<bool>(func, AtomicSubscribeLock);
        }
    }
}
