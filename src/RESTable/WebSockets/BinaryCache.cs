using System.Collections.Concurrent;
using System.Threading;

namespace RESTable.WebSockets
{
    internal class BinaryCache : ConcurrentDictionary<object, CacheItem>
    {
        internal bool TryGet(object key, out byte[] cached)
        {
            try
            {
                if (TryGetValue(key, out var cacheItem))
                {
                    cacheItem.Nudge();
                    cached = cacheItem.Binary;
                    return true;
                }
                cached = null;
                return false;
            }
            catch
            {
                cached = null;
                return false;
            }
        }

        internal void Cache(object key, byte[] binary)
        {
            try
            {
                var timer = new Timer(state =>
                {
                    TryRemove(key, out _);
                    ((Timer) state).Dispose();
                });
                var cacheItem = new CacheItem(binary, timer);
                cacheItem.Nudge();
                TryAdd(key, cacheItem);
            }
            catch { }
        }
    }
}