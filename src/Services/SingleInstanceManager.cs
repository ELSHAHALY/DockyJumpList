using System;
using System.Threading;

namespace DockyJumpList.Services
{
    /// <summary>
    /// Ensures only one instance of Docky runs at a time.
    /// Call Acquire() at startup — if it returns false, exit immediately.
    /// </summary>
    public sealed class SingleInstanceManager : IDisposable
    {
        private const string MutexName = "DockyJumpList_SingleInstance_Mutex";
        private Mutex _mutex;
        private bool _owned;

        /// <summary>
        /// Tries to acquire the global mutex.
        /// Returns true if this is the first instance, false if another is already running.
        /// </summary>
        public bool Acquire()
        {
            _mutex = new Mutex(initiallyOwned: true, name: MutexName, out _owned);
            return _owned;
        }

        public void Dispose()
        {
            if (_owned)
            {
                try { _mutex?.ReleaseMutex(); } catch { /* already released */ }
            }
            _mutex?.Dispose();
        }
    }
}
