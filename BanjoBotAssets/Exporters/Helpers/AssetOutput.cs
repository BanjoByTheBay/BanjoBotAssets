using BanjoBotAssets.Exporters.Helpers;
using System.Collections.Concurrent;

namespace BanjoBotAssets.Models
{
    internal class AssetOutput : IAssetOutput
    {
        private ItemRatingTable? defaultItemRatings, survivorItemRatings, leadSurvivorItemRatings;
        private readonly ConcurrentDictionary<string, NamedItemData> namedItems = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, DifficultyInfo> difficultyInfo = new(StringComparer.OrdinalIgnoreCase);

        private readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, int>> craftingRecipes = new(StringComparer.OrdinalIgnoreCase);

        public void AddDefaultItemRatings(ItemRatingTable itemRatings)
        {
            defaultItemRatings = itemRatings;
        }

        public void AddDifficultyInfo(string name, DifficultyInfo difficultyInfo)
        {
            this.difficultyInfo.TryAdd(name, difficultyInfo);
        }

        public void AddLeadSurvivorItemRatings(ItemRatingTable itemRatings)
        {
            leadSurvivorItemRatings = itemRatings;
        }

        public void AddNamedItem(string name, NamedItemData itemData)
        {
            namedItems.TryAdd(name, itemData);
        }

        public void AddSurvivorItemRatings(ItemRatingTable itemRatings)
        {
            survivorItemRatings = itemRatings;
        }

        public void CopyTo(ExportedAssets exportedAssets, IList<ExportedRecipe> exportedRecipes, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var (k, v) in namedItems)
            {
                exportedAssets.NamedItems.TryAdd(k, v);
            }

            cancellationToken.ThrowIfCancellationRequested();

            foreach (var (k, v) in difficultyInfo)
            {
                exportedAssets.DifficultyInfo.TryAdd(k, v);
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (defaultItemRatings != null)
                exportedAssets.ItemRatings.Default = defaultItemRatings;
            if (survivorItemRatings != null)
                exportedAssets.ItemRatings.Survivor = survivorItemRatings;
            if (leadSurvivorItemRatings != null)
                exportedAssets.ItemRatings.LeadSurvivor = leadSurvivorItemRatings;

            foreach (var (templateId, recipe) in craftingRecipes)
            {
                var ingredients = new Queue<KeyValuePair<string, int>>(recipe);

                var exportedRecipe = new ExportedRecipe { ItemName = templateId };

                if (ingredients.TryDequeue(out var pair))
                    (exportedRecipe.Ingredient1, exportedRecipe.Quantity1!) = pair;
                if (ingredients.TryDequeue(out pair))
                    (exportedRecipe.Ingredient2, exportedRecipe.Quantity2!) = pair;
                if (ingredients.TryDequeue(out pair))
                    (exportedRecipe.Ingredient3, exportedRecipe.Quantity3!) = pair;
                if (ingredients.TryDequeue(out pair))
                    (exportedRecipe.Ingredient4, exportedRecipe.Quantity4!) = pair;
                if (ingredients.TryDequeue(out pair))
                    (exportedRecipe.Ingredient5, exportedRecipe.Quantity5!) = pair;

                exportedRecipes.Add(exportedRecipe);

                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        public void AddCraftingRecipe(string name, IReadOnlyDictionary<string, int> ingredients)
        {
            craftingRecipes.TryAdd(name, ingredients);
        }
    }
}
