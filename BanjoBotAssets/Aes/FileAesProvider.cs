using Microsoft.Extensions.Options;
using System.Text.Json;

namespace BanjoBotAssets.Aes
{
    internal class FileAesProvider : IAesProvider, IAesCacheUpdater
    {
        private readonly ILogger<FileAesProvider> logger;
        private readonly IOptions<AesOptions> options;

        public FileAesProvider(ILogger<FileAesProvider> logger, IOptions<AesOptions> options)
        {
            this.logger = logger;
            this.options = options;
        }

        public async Task<AesApiResponse?> TryGetAesAsync(CancellationToken cancellationToken)
        {
            if (!File.Exists(options.Value.LocalFilePath))
            {
                logger.LogInformation(Resources.Status_MissingAesCache, options.Value.LocalFilePath);
                return null;
            }

            logger.LogInformation(Resources.Status_LoadingAesFromFile, options.Value.LocalFilePath);
            return JsonSerializer.Deserialize<AesApiResponse>(await File.ReadAllTextAsync(options.Value.LocalFilePath, cancellationToken));
        }

        public Task UpdateAesCacheAsync(AesApiResponse response, CancellationToken cancellationToken)
        {
            logger.LogInformation(Resources.Status_SavingAesToFile, options.Value.LocalFilePath);
            return File.WriteAllTextAsync(options.Value.LocalFilePath, JsonSerializer.Serialize(response), cancellationToken);
        }
    }
}
