using System;
using System.Collections.Concurrent;

namespace MemoryEngine.Core
{
    public class CacheManager
    {
        private class CacheItem
        {
            public object Value { get; set; }
            public long ExpirationTicks { get; set; }
        }

        private readonly ConcurrentDictionary<string, CacheItem> _cache = new();
        private readonly int _defaultExpirationMs;

        public CacheManager(int defaultExpirationMs = 16) // 16ms = ~60 Ticks pro Sekunde
        {
            _defaultExpirationMs = defaultExpirationMs;
        }

        // Holt einen Wert aus dem Cache. Wenn er abgelaufen oder nicht vorhanden ist, 
        // wird die updateFunc ausgeführt, um ihn neu aus dem Speicher zu lesen.
        public T GetOrUpdate<T>(string key, Func<T> updateFunc, int? overrideExpirationMs = null)
        {
            long currentTicks = Environment.TickCount64;

            if (_cache.TryGetValue(key, out var item))
            {
                if (currentTicks < item.ExpirationTicks)
                {
                    return (T)item.Value;
                }
            }

            // Wert ist abgelaufen oder existiert nicht -> Neu auslesen
            T newValue = updateFunc();

            int expiration = overrideExpirationMs ?? _defaultExpirationMs;
            _cache[key] = new CacheItem
            {
                Value = newValue,
                ExpirationTicks = currentTicks + expiration
            };

            return newValue;
        }

        public void Invalidate(string key)
        {
            _cache.TryRemove(key, out _);
        }
    }
}