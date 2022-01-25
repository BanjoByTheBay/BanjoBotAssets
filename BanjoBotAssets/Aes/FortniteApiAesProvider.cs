using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace BanjoBotAssets.Aes
{
    internal class FortniteApiAesProvider : IAesProvider
    {
        private readonly ILogger<FortniteApiAesProvider> logger;
        private readonly IOptions<AesOptions> options;
        private readonly IHttpClientFactory httpClientFactory;

        public FortniteApiAesProvider(ILogger<FortniteApiAesProvider> logger, IOptions<AesOptions> options, IHttpClientFactory httpClientFactory)
        {
            this.logger = logger;
            this.options = options;
            this.httpClientFactory = httpClientFactory;
        }

        public async Task<AesApiResponse?> TryGetAesAsync(CancellationToken cancellationToken)
        {
            using var client = httpClientFactory.CreateClient();
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                logger.LogInformation(Resources.Status_FetchingAesFromApi, options.Value.AesApiUri);
                return await client.GetFromJsonAsync<AesApiResponse>(options.Value.AesApiUri, cancellationToken);
            }
            catch (Exception)
            {
                // pokemon
                return null;
            }
        }
    }
}
