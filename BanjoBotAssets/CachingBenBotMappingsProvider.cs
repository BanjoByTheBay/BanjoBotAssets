using BanjoBotAssets.Config;
using CUE4Parse.MappingsProvider;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System.Runtime.InteropServices;

namespace BanjoBotAssets
{
    internal interface ITypeMappingsProviderFactory
    {
        ITypeMappingsProvider Create(string gameName, string? specificVersion = null);
    }

    internal class CachingBenBotMappingsProviderFactory : ITypeMappingsProviderFactory
    {
        private readonly IServiceProvider serviceProvider;
        private readonly ObjectFactory objectFactory;

        public CachingBenBotMappingsProviderFactory(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;

            objectFactory = ActivatorUtilities.CreateFactory(
                typeof(CachingBenBotMappingsProvider),
                new[] { typeof(string), typeof(string) });
        }

        public ITypeMappingsProvider Create(string gameName, string? specificVersion = null)
        {
            return (ITypeMappingsProvider)objectFactory.Invoke(serviceProvider, new[] { gameName, specificVersion });
        }
    }

    internal sealed class CachingBenBotMappingsProvider : UsmapTypeMappingsProvider
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly ILogger<CachingBenBotMappingsProvider> logger;
        private readonly IOptions<MappingsOptions> options;
        private readonly GameDirectoryProvider gameDirectoryProvider;
        private readonly string? _specificVersion;
        private readonly string _gameName;
        private readonly bool _isWindows64Bit;

        public CachingBenBotMappingsProvider(IHttpClientFactory httpClientFactory, ILogger<CachingBenBotMappingsProvider> logger,
            IOptions<MappingsOptions> options, GameDirectoryProvider gameDirectoryProvider, string gameName, string? specificVersion = null)
        {
            this.httpClientFactory = httpClientFactory;
            this.logger = logger;
            this.options = options;
            this.gameDirectoryProvider = gameDirectoryProvider;
            _specificVersion = specificVersion;
            _gameName = gameName;
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

        public override bool Reload()
        {
            return ReloadAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public override async Task<bool> ReloadAsync()
        {
            try
            {
                string cacheFile = options.Value.LocalFilePath;
                bool hasCachedFallback = false;

                async Task<bool> LoadCachedMappings()
                {
                    var bytes = await File.ReadAllBytesAsync(cacheFile);
                    AddUsmap(bytes, _gameName, cacheFile);
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

                AddUsmap(usmapBytes, _gameName, usmapName!);
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