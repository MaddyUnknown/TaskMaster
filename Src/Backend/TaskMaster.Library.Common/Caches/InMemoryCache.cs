using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using TaskMaster.Library.Common.Interfaces.Caches;

namespace TaskMaster.Library.Common.Caches
{
    internal class InMemoryCache : ICache
    {
        private readonly MemoryCache _cache;
        private readonly long _maxEntries;
        private readonly double _compactPercentage;

        public InMemoryCache(MemoryCache cache, IOptions<MemoryCacheOptions> options)
        {
            _cache = cache;
            _maxEntries = (long) (options.Value.SizeLimit ?? 200);
            _compactPercentage = options.Value.CompactionPercentage;
        }

        public T GetOrAdd<T>(string key, Func<T> factory, TimeSpan? expiry = null)
        {
            if (_cache.TryGetValue(key, out var value)) return (T)value!;

            if (_cache.Count >= _maxEntries) _cache.Compact(percentage: _compactPercentage);

            return _cache.GetOrCreate(key, entry =>
            {
                entry.Size = 1;
                entry.Priority = CacheItemPriority.Normal;
                if (expiry.HasValue) entry.AbsoluteExpirationRelativeToNow = expiry;

                return factory();
            })!;
        }

        public async Task<T> GetOrAddAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null)
        {
            if (_cache.TryGetValue(key, out var value)) return (T)value!;

            if (_cache.Count >= _maxEntries) _cache.Compact(percentage: _compactPercentage);

            return (await _cache.GetOrCreateAsync(key, async entry =>
            {
                entry.Size = 1;
                entry.Priority = CacheItemPriority.Normal;
                if (expiry.HasValue) entry.AbsoluteExpirationRelativeToNow = expiry;

                return await factory();
            }))!;
        }
    }
}
