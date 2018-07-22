using System;

namespace Data
{
    public interface IAtomicOperation
    {
        bool IsTransferingData { get; set; }

        void PerformAsAtomicFunction(Action func, bool safeToPassLock = false);

        T PerformAsAtomicFunction<T>(Func<T> func, bool safeToPassLock = false);

        void PerformAsAtomicDownload(Action func);

        T PerformAsAtomicUpload<T>(Func<T> func);

        bool PerformAsAtomicSubscribe(Func<bool> func);
    }
}
