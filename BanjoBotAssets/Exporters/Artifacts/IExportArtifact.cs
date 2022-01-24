using BanjoBotAssets.Models;

namespace BanjoBotAssets.Exporters
{
    internal interface IExportArtifact
    {
        Task RunAsync(ExportedAssets exportedAssets, IList<ExportedRecipe> exportedRecipes,
            CancellationToken cancellationToken = default);
    }
}
