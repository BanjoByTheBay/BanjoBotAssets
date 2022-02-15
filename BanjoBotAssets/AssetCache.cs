using BanjoBotAssets.Config;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace BanjoBotAssets
{
    internal class AssetCache
    {
        public MemoryCache Cache { get; }

        public AssetCache(IOptions<CacheOptions> options)
        {
            Cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = options.Value.SizeLimit });
        }
    }
}
