using BanjoBotAssets.Artifacts.Models;

namespace BanjoBotAssets.PostExporters
{
    internal interface IPostExporter : IAssetCounter
    {
        Task ProcessExportsAsync(ExportedAssets exportedAssets, IList<ExportedRecipe> exportedRecipes, CancellationToken cancellationToken = default);

        int AssetsLoaded { get; }
    }
}
