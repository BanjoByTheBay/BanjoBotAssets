using BanjoBotAssets.Config;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace BanjoBotAssets.Aes
{
    internal sealed class FileAesProvider : IAesProvider, IAesCacheUpdater
    {
        private readonly ILogger<FileAesProvider> logger;
        private readonly IOptions<AesOptions> options;
        private readonly GameDirectoryProvider gameDirectoryProvider;

        public FileAesProvider(ILogger<FileAesProvider> logger, IOptions<AesOptions> options, GameDirectoryProvider gameDirectoryProvider)
        {
            this.logger = logger;
            this.options = options;
            this.gameDirectoryProvider = gameDirectoryProvider;
        }

        public async Task<AesApiResponse?> TryGetAesAsync(CancellationToken cancellationToken)
        {
            string localFilePath = options.Value.LocalFilePath;

            if (!File.Exists(localFilePath))
            {
                logger.LogInformation(Resources.Status_MissingAesCache, localFilePath);
                return null;
            }

            if (File.GetLastWriteTime(localFilePath) < gameDirectoryProvider.GetGameDirectory().LastWriteTime)
            {
                logger.LogInformation(Resources.Status_SkippingOutdatedCachedAes, localFilePath);
                return null;
            }

            logger.LogInformation(Resources.Status_LoadingAesFromFile, localFilePath);
            return JsonSerializer.Deserialize<AesApiResponse>(await File.ReadAllTextAsync(localFilePath, cancellationToken));
        }

        public Task UpdateAesCacheAsync(AesApiResponse response, CancellationToken cancellationToken)
        {
            logger.LogInformation(Resources.Status_SavingAesToFile, options.Value.LocalFilePath);
            return File.WriteAllTextAsync(options.Value.LocalFilePath, JsonSerializer.Serialize(response), cancellationToken);
        }
    }
}
