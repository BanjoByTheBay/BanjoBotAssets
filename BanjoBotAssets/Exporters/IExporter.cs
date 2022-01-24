using BanjoBotAssets.Exporters.Helpers;

namespace BanjoBotAssets.Exporters
{
    internal interface IExporter : IAssetCounter
    {
        void ObserveAsset(string name);

        Task ExportAssetsAsync(IProgress<ExportProgress> progress, IAssetOutput output, CancellationToken cancellationToken);

        int AssetsLoaded { get; }
    }
}
