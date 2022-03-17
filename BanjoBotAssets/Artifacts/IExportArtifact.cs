using BanjoBotAssets.Artifacts.Models;

namespace BanjoBotAssets.Artifacts
{
    internal interface IExportArtifact
    {
        Task RunAsync(ExportedAssets exportedAssets, IList<ExportedRecipe> exportedRecipes,
            CancellationToken cancellationToken = default);
    }
}
