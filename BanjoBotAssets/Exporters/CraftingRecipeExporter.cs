namespace BanjoBotAssets.Exporters
{
    internal class CraftingRecipeExporter : BaseExporter
    {
        public CraftingRecipeExporter(DefaultFileProvider provider) : base(provider)
        {
        }

        private static readonly Regex widOrTidRegex = new(@"^[tw]id_", RegexOptions.IgnoreCase);

        public override async Task ExportAssetsAsync(IProgress<ExportProgress> progress, IAssetOutput output)
        {
            Interlocked.Increment(ref assetsLoaded);

            var file = provider[assetPaths[0]];
            var craftingTable = await provider.LoadObjectAsync<UDataTable>(file.PathWithoutExtension);
            var numToProcess = craftingTable.RowMap.Count;
            var processedSoFar = 0;

            progress.Report(new ExportProgress { AssetsLoaded = assetsLoaded, CompletedSteps = 0, TotalSteps = numToProcess, CurrentItem = "Exporting recipes" });

            foreach (var (key, recipe) in craftingTable.RowMap)
            {
                Interlocked.Increment(ref processedSoFar);
                progress.Report(new ExportProgress { AssetsLoaded = assetsLoaded, CompletedSteps = processedSoFar, TotalSteps = numToProcess, CurrentItem = key.Text });

                var recipeResults = recipe.GetOrDefault<FFortItemQuantityPair[]>("RecipeResults");
                var assetName = recipeResults[0].ItemPrimaryAssetId.PrimaryAssetName.Text;

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

            progress.Report(new ExportProgress { AssetsLoaded = assetsLoaded, CompletedSteps = processedSoFar, TotalSteps = numToProcess, CurrentItem = "Exported recipes" });
        }

        protected override bool InterestedInAsset(string name) => name.Contains("/CraftingRecipes_New");
    }
}
