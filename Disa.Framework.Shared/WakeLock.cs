using System;

namespace Disa.Framework
{
    public abstract class WakeLock : IDisposable
    {
        public abstract void Dispose();

        public abstract void TemporaryAcquire();

        public abstract void TemporaryRelease();

        public class TemporaryFree : IDisposable
        {
            private readonly WakeLock _wakeLock;

            public TemporaryFree(WakeLock wakeLock)
            {
                _wakeLock = wakeLock;
                _wakeLock.TemporaryRelease();
            }

            public void Dispose()
            {
                _wakeLock.TemporaryAcquire();
            }
        }
    }
}