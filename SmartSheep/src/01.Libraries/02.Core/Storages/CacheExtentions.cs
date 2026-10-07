using Microsoft.Extensions.Caching.Memory;
using Project.Base;

namespace Project.Core.Storages
{
    public static class CacheExtentions
    {
        public static bool IsExistCache(this string key, IMemoryCache memoryCache)
        {
            return memoryCache.TryGetValue(key, out string _);
        }

        public static string GetCache(this string key, IMemoryCache memoryCache)
        {
            memoryCache.TryGetValue(key, out string value);
            return value;
        }

        public static void SetCache(this KeyValue model, double absoluteExpiration, double slidingExpiration, IMemoryCache memoryCache)
        {
            var cacheExpiryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = DateTime.Now.AddDays(absoluteExpiration),
                Priority = CacheItemPriority.High,
                SlidingExpiration = TimeSpan.FromDays(slidingExpiration),
                Size = 1024, //function as the limit on the number of entries
            };

            memoryCache.Set(model.Key, model.Value, cacheExpiryOptions);
        }

        public static void RemoveCache(this string key, IMemoryCache memoryCache)
        {
            memoryCache.Remove(key);
        }
    }
}
