using TaskMaster.Library.Common.Interfaces.Caches;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskMaster.Library.Common.Constants;

namespace TaskMaster.Library.Common.Caches
{
    internal class InMemoryCache : ICache
    {
        private ConcurrentDictionary<string, object> _cache;

        internal InMemoryCache()
        {
            _cache = new ConcurrentDictionary<string, object>();
        }

        public T GetOrAdd<T>(string key, Func<T>? factory = null)
        {
            if (_cache.TryGetValue(key, out var value))
            {
                return (T)value;
            }

            if (factory == null)
            {
                throw new Exception(ErrorMessage.CacheKeyNotFound(key));
            }

            var createdValue = factory();
            if (createdValue == null)
            {
                throw new Exception(ErrorMessage.CacheKeyNotFound(key));
            }

            return (T)_cache.GetOrAdd(key, createdValue);
        }
    }
}
