using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Versions;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace BanjoBotAssets
{
    internal class CachingFileProvider : DefaultFileProvider
    {
        private readonly AssetCache cache;
        private readonly ILogger<CachingFileProvider> logger;
        private int cacheRequests, cacheMisses;
        private readonly ConcurrentDictionary<string, int> cacheMissesByPath = new();

        public CachingFileProvider(AssetCache cache, ILogger<CachingFileProvider> logger, string directory, SearchOption searchOption, bool isCaseInsensitive = false, VersionContainer? versions = null) : base(directory, searchOption, isCaseInsensitive, versions)
        {
            this.cache = cache;
            this.logger = logger;
        }

        public CachingFileProvider(AssetCache cache, ILogger<CachingFileProvider> logger, DirectoryInfo directory, SearchOption searchOption, bool isCaseInsensitive = false, VersionContainer? versions = null) : base(directory, searchOption, isCaseInsensitive, versions)
        {
            this.cache = cache;
            this.logger = logger;
        }

        public CachingFileProvider(AssetCache cache, ILogger<CachingFileProvider> logger, DirectoryInfo mainDirectory, List<DirectoryInfo> extraDirectories, SearchOption searchOption, bool isCaseInsensitive = false, VersionContainer? versions = null) : base(mainDirectory, extraDirectories, searchOption, isCaseInsensitive, versions)
        {
            this.cache = cache;
            this.logger = logger;
        }

        public override Task<IPackage> LoadPackageAsync(GameFile file)
        {
            Interlocked.Increment(ref cacheRequests);

            return cache.Cache.GetOrAddAsync(
                file.Path,
                async _ =>
                {
                    Interlocked.Increment(ref cacheMisses);

                    cacheMissesByPath.AddOrUpdate(file.Path, 1, (_, i) => i + 1);
                    logger.LogDebug(Resources.Status_CacheMiss, file.Path, file.Size);

                    return await base.LoadPackageAsync(file);
                },
                new MemoryCacheEntryOptions { Size = file.Size });
        }

        public override Task<IPackage?> TryLoadPackageAsync(GameFile file)
        {
            Interlocked.Increment(ref cacheRequests);

            return cache.Cache.GetOrAddAsync(
                file.Path,
                async _ =>
                {
                    Interlocked.Increment(ref cacheMisses);

                    cacheMissesByPath.AddOrUpdate(file.Path, 1, (_, i) => i + 1);
                    logger.LogDebug(Resources.Status_CacheMiss, file.Path, file.Size);

                    return await base.TryLoadPackageAsync(file);
                },
                new MemoryCacheEntryOptions { Size = file.Size });
        }

        public void ReportCacheStats()
        {
            logger.LogInformation(Resources.Status_CacheStats,
                cache.CachedItemCount,
                cacheRequests - cacheMisses,
                cacheMisses,
                cacheRequests == 0 ? 0 : (cacheRequests - cacheMisses) / (float)cacheRequests);

            const int MaxTops = 10;

            var topMisses = (from pair in cacheMissesByPath
                             orderby pair.Value descending
                             select pair).Take(MaxTops);
            logger.LogDebug(Resources.Status_CacheTopMisses_Heading);
            foreach (var (path, count) in topMisses)
            {
                logger.LogDebug(Resources.Status_CacheTopMisses_Entry, count, path);
            }
        }
    }
}
