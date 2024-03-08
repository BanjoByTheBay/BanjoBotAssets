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
using BanjoBotAssets.Config;
using CUE4Parse.MappingsProvider;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System.Runtime.InteropServices;

namespace BanjoBotAssets
{
    internal interface ITypeMappingsProviderFactory
    {
        ITypeMappingsProvider Create(string? specificVersion = null);
    }

    internal sealed class CachingFNCentralMappingsProviderFactory(IServiceProvider serviceProvider) : ITypeMappingsProviderFactory
    {
        private readonly ObjectFactory objectFactory = ActivatorUtilities.CreateFactory(
                typeof(CachingFNCentralMappingsProvider),
                [typeof(string)]);

        public ITypeMappingsProvider Create(string? specificVersion = null)
        {
            return (ITypeMappingsProvider)objectFactory.Invoke(serviceProvider, [specificVersion]);
        }
    }

    internal sealed class CachingFNCentralMappingsProvider : UsmapTypeMappingsProvider
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly ILogger<CachingFNCentralMappingsProvider> logger;
        private readonly IOptions<MappingsOptions> options;
        private readonly GameDirectoryProvider gameDirectoryProvider;
        private readonly string? _specificVersion;
        private readonly bool _isWindows64Bit;

        public CachingFNCentralMappingsProvider(IHttpClientFactory httpClientFactory, ILogger<CachingFNCentralMappingsProvider> logger,
            IOptions<MappingsOptions> options, GameDirectoryProvider gameDirectoryProvider, string? specificVersion = null)
        {
            this.httpClientFactory = httpClientFactory;
            this.logger = logger;
            this.options = options;
            this.gameDirectoryProvider = gameDirectoryProvider;
            _specificVersion = specificVersion;
            _isWindows64Bit = Environment.Is64BitOperatingSystem && RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            Reload();
        }

        private HttpClient CreateHttpClient()
        {
            var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(2);
            client.DefaultRequestHeaders.Add("User-Agent", "CUE4Parse");
            return client;
        }

        public override void Reload()
        {
            ReloadAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public async Task<bool> ReloadAsync()
        {
            try
            {
                string cacheFile = options.Value.LocalFilePath;
                bool hasCachedFallback = false;

                async Task<bool> LoadCachedMappings()
                {
                    var bytes = await File.ReadAllBytesAsync(cacheFile);
                    Load(bytes);
                    logger.LogInformation(Resources.Status_LoadedCachedMappings, cacheFile);
                    return true;
                }

                if (File.Exists(cacheFile))
                {
                    if (File.GetLastWriteTime(cacheFile) < gameDirectoryProvider.GetGameDirectory().LastWriteTime)
                    {
                        logger.LogInformation(Resources.Status_SkippingOutdatedCachedMappings, cacheFile);
                        hasCachedFallback = true;
                    }
                    else
                    {
                        return await LoadCachedMappings();
                    }
                }

                var jsonText = _specificVersion != null
                    ? await LoadEndpoint(options.Value.MappingsApiUri + $"?version={_specificVersion}")
                    : await LoadEndpoint(options.Value.MappingsApiUri);
                if (jsonText == null)
                {
                    logger.LogError(Resources.Error_FailedToGetMappingsEndpoint);
                    return hasCachedFallback && await LoadCachedMappings();
                }
                var json = JArray.Parse(jsonText);
                var preferredCompression = _isWindows64Bit ? "Oodle" : "Brotli";

                if (!json.HasValues)
                {
                    logger.LogError(Resources.Error_MissingMappingsJsonArray);
                    return hasCachedFallback && await LoadCachedMappings();
                }

                string? usmapUrl = null;
                string? usmapName = null;
                foreach (var arrayEntry in json)
                {
                    var method = arrayEntry["meta"]?["compressionMethod"]?.ToString();
                    if (method != null && method == preferredCompression)
                    {
                        usmapUrl = arrayEntry["url"]?.ToString();
                        usmapName = arrayEntry["fileName"]?.ToString();
                        break;
                    }
                }

                if (usmapUrl == null)
                {
                    usmapUrl = json[0]["url"]?.ToString()!;
                    usmapName = json[0]["fileName"]?.ToString()!;
                }

                var usmapBytes = await LoadEndpointBytes(usmapUrl);
                if (usmapBytes == null)
                {
                    logger.LogError(Resources.Error_FailedToDownloadUsmap, usmapUrl);
                    return hasCachedFallback && await LoadCachedMappings();
                }

                await File.WriteAllBytesAsync(cacheFile, usmapBytes);
                logger.LogInformation(Resources.Status_CachedMappingsToFile, cacheFile);

                Load(usmapBytes);
                return true;
            }
            catch (Exception e)
            {
                logger.LogError(e, Resources.Error_UncaughtExceptionWhileReloadingMappings);
                return false;
            }
        }

        private async Task<string?> LoadEndpoint(string url)
        {
            try
            {
                using var client = CreateHttpClient();
                return await client.GetStringAsync(url);
            }
            catch
            {
                return null;
            }
        }

        private async Task<byte[]?> LoadEndpointBytes(string url)
        {
            try
            {
                using var client = CreateHttpClient();
                return await client.GetByteArrayAsync(url);
            }
            catch
            {
                return null;
            }
        }
    }
}