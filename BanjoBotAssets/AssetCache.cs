using BanjoBotAssets.Config;
using LazyCache;
using LazyCache.Providers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace BanjoBotAssets
{
    internal sealed class AssetCache : IDisposable
    {
        private readonly MemoryCache memoryCache;

        public CachingService Cache { get; }

        public int CachedItemCount => memoryCache.Count;

        public AssetCache(IOptions<CacheOptions> options, ILoggerFactory loggerFactory)
        {
            memoryCache = new MemoryCache(new MemoryCacheOptions { SizeLimit = options.Value.SizeLimit }, loggerFactory);
            Cache = new CachingService(new MemoryCacheProvider(memoryCache));
        }

        public void Dispose()
        {
            memoryCache.Dispose();
        }
    }
}
