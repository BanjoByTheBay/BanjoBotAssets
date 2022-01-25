namespace BanjoBotAssets.Aes
{
    internal interface IAesProvider
    {
        Task<AesApiResponse?> TryGetAesAsync(CancellationToken cancellationToken);
    }

    internal interface IAesCacheUpdater
    {
        Task UpdateAesCacheAsync(AesApiResponse response, CancellationToken cancellationToken);
    }
}
