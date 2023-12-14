/* Copyright 2023 Tara "Dino" Cassatt
 * 
 * This file is part of BanjoBotAssets.
 * 
 * BanjoBotAssets is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * BanjoBotAssets is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with BanjoBotAssets.  If not, see <http://www.gnu.org/licenses/>.
 */
using CUE4Parse.FileProvider;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.Versions;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace BanjoBotAssets
{
    internal sealed class CachingFileProvider : DefaultFileProvider
    {
        private readonly AssetCache cache;
        private readonly ILogger<CachingFileProvider> logger;
        private int cacheRequests, cacheMisses;
        private readonly ConcurrentDictionary<string, int> cacheMissesByPath = new();

        private readonly string? assetLogPath;
        private TextWriter? assetLogWriter;
        private FileStream? assetLogStream;

        public CachingFileProvider(AssetCache cache, ILogger<CachingFileProvider> logger, string directory, SearchOption searchOption, bool isCaseInsensitive = false, VersionContainer? versions = null, string? assetLogPath = null) : base(directory, searchOption, isCaseInsensitive, versions)
        {
            this.cache = cache;
            this.logger = logger;
            this.assetLogPath = assetLogPath;
            OpenAssetLog();
        }

        public CachingFileProvider(AssetCache cache, ILogger<CachingFileProvider> logger, DirectoryInfo directory, SearchOption searchOption, bool isCaseInsensitive = false, VersionContainer? versions = null, string? assetLogPath = null) : base(directory, searchOption, isCaseInsensitive, versions)
        {
            this.cache = cache;
            this.logger = logger;
            this.assetLogPath = assetLogPath;
            OpenAssetLog();
        }

        public CachingFileProvider(
            AssetCache cache,
            ILogger<CachingFileProvider> logger,
            DirectoryInfo mainDirectory,
            DirectoryInfo[] extraDirectories,
            SearchOption searchOption,
            bool isCaseInsensitive = false,
            VersionContainer? versions = null,
            string? assetLogPath = null) : base(mainDirectory, extraDirectories, searchOption, isCaseInsensitive, versions)
        {
            this.cache = cache;
            this.logger = logger;
            this.assetLogPath = assetLogPath;
            OpenAssetLog();
        }

        private void OpenAssetLog()
        {
            if (assetLogPath == null)
            {
                return;
            }

            assetLogStream = new FileStream(assetLogPath, new FileStreamOptions
            {
                Access = FileAccess.Write,
                BufferSize = 0,
                Mode = FileMode.Create,
                Options = FileOptions.WriteThrough,
                Share = FileShare.Write,
            });

            assetLogWriter = new StreamWriter(assetLogStream) { AutoFlush = true };
        }

        private void WriteToAssetLog(string line)
        {
            if (assetLogWriter == null)
            {
                return;
            }

            lock (assetLogWriter)
            {
                assetLogWriter.WriteLine(line);
            }
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

                    WriteToAssetLog(file.Path);
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

                    WriteToAssetLog(file.Path);
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
