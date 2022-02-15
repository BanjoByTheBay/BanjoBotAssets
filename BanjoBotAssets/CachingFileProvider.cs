using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Versions;
using Microsoft.Extensions.Caching.Memory;

namespace BanjoBotAssets
{
    internal class CachingFileProvider : DefaultFileProvider
    {
        private readonly AssetCache cache;
        private int cacheRequests, cacheMisses;

        public CachingFileProvider(AssetCache cache, string directory, SearchOption searchOption, bool isCaseInsensitive = false, VersionContainer? versions = null) : base(directory, searchOption, isCaseInsensitive, versions)
        {
            this.cache = cache;
        }

        public CachingFileProvider(AssetCache cache, DirectoryInfo directory, SearchOption searchOption, bool isCaseInsensitive = false, VersionContainer? versions = null) : base(directory, searchOption, isCaseInsensitive, versions)
        {
            this.cache = cache;
        }

        public CachingFileProvider(AssetCache cache, DirectoryInfo mainDirectory, List<DirectoryInfo> extraDirectories, SearchOption searchOption, bool isCaseInsensitive = false, VersionContainer? versions = null) : base(mainDirectory, extraDirectories, searchOption, isCaseInsensitive, versions)
        {
            this.cache = cache;
        }

        public override Task<IPackage> LoadPackageAsync(GameFile file)
        {
            Interlocked.Increment(ref cacheRequests);

            return cache.Cache.GetOrCreateAsync(file, async cacheEntry =>
            {
                Interlocked.Increment(ref cacheMisses);

                cacheEntry.SetSize(file.Size);
                return await base.LoadPackageAsync(file);
            });
        }

        public override Task<IPackage?> TryLoadPackageAsync(GameFile file)
        {
            Interlocked.Increment(ref cacheRequests);

            return cache.Cache.GetOrCreateAsync(file, async cacheEntry =>
            {
                Interlocked.Increment(ref cacheMisses);

                cacheEntry.SetSize(file.Size);
                return await base.TryLoadPackageAsync(file);
            });
        }

        public void ReportCacheStats()
        {
            System.Diagnostics.Debug.WriteLine("Cache hits: {0}. Misses: {1}. Hit ratio: {2:0.0%}.",
                cacheRequests - cacheMisses,
                cacheMisses,
                cacheRequests == 0 ? 0 : (cacheRequests - cacheMisses) / (float)cacheRequests);
        }
    }
}
