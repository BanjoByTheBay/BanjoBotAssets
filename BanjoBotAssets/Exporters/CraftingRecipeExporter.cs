namespace BanjoBotAssets.Exporters
{
    internal sealed partial class CraftingRecipeExporter : BaseExporter
    {
        private static readonly Regex widOrTidRegex = WidOrTidRegex();

        public CraftingRecipeExporter(IExporterContext services) : base(services)
        {
        }

        public override async Task ExportAssetsAsync(IProgress<ExportProgress> progress, IAssetOutput output, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref assetsLoaded);

            var file = provider[assetPaths[0]];
            var craftingTable = await provider.LoadObjectAsync<UDataTable>(file.PathWithoutExtension);
            var numToProcess = craftingTable.RowMap.Count;
            var processedSoFar = 0;

            progress.Report(new ExportProgress { AssetsLoaded = assetsLoaded, CompletedSteps = 0, TotalSteps = numToProcess, CurrentItem = Resources.Status_ExportingRecipes });

            foreach (var (key, recipe) in craftingTable.RowMap)
            {
                Interlocked.Increment(ref processedSoFar);
                progress.Report(new ExportProgress { AssetsLoaded = assetsLoaded, CompletedSteps = processedSoFar, TotalSteps = numToProcess, CurrentItem = key.Text });

                var recipeResults = recipe.GetOrDefault<FFortItemQuantityPair[]>("RecipeResults");
                var assetName = recipeResults[0].ItemPrimaryAssetId.PrimaryAssetName.Text;

                /**
                 * NOTE: we store template IDs instead of display names in the recipes here.
                 * they're replaced with display names in <see cref="Artifacts.SchematicsJsonArtifact"/>.
                 */

                // for weapons and traps, find the schematic by replacing the wid_ or tid_ prefix with sid_
                var templateId = widOrTidRegex.Replace(assetName, "Schematic:sid_");
                if (templateId == assetName)
                {
                    // otherwise, assume the name doesn't change
                    templateId = $"{recipeResults[0].ItemPrimaryAssetId.PrimaryAssetType.Name.Text}:{templateId}";
                }

                var recipeCosts = recipe.GetOrDefault<FFortItemQuantityPair[]>("RecipeCosts");
                var ingredients = recipeCosts.ToDictionary(
                    p => $"{p.ItemPrimaryAssetId.PrimaryAssetType.Name.Text}:{p.ItemPrimaryAssetId.PrimaryAssetName.Text}",
                    p => p.Quantity,
                    StringComparer.OrdinalIgnoreCase);

                output.AddCraftingRecipe(templateId, ingredients);
            }

            progress.Report(new ExportProgress { AssetsLoaded = assetsLoaded, CompletedSteps = processedSoFar, TotalSteps = numToProcess, CurrentItem = Resources.Status_ExportedRecipes });
        }

        protected override bool InterestedInAsset(string name) =>
            name.Contains("/CraftingRecipes_New", StringComparison.OrdinalIgnoreCase);
        [GeneratedRegex("^[tw]id_", RegexOptions.IgnoreCase, "en-US")]
        private static partial Regex WidOrTidRegex();
    }
}
