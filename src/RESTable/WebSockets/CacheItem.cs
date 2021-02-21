using System.Threading;

namespace RESTable.WebSockets
{
    internal struct CacheItem
    {
        private const int ExpirationMilliseconds = 3000;
        internal byte[] Binary { get; }
        private Timer Timer { get; }
        internal CacheItem(byte[] binary, Timer timer) => (Binary, Timer) = (binary, timer);
        internal void Nudge() => Timer.Change(ExpirationMilliseconds, Timeout.Infinite);
    }
}