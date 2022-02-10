using BanjoBotAssets.Artifacts;
using System.Collections.Concurrent;

namespace BanjoBotAssets.Exporters.Helpers
{
    internal class AssetOutput : IAssetOutput
    {
        private ItemRatingTable? defaultItemRatings, survivorItemRatings, leadSurvivorItemRatings;
        private readonly ConcurrentDictionary<string, NamedItemData> namedItems = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, DifficultyInfo> difficultyInfo = new(StringComparer.OrdinalIgnoreCase);

        private readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, int>> craftingRecipes = new(StringComparer.OrdinalIgnoreCase);

        private readonly ConcurrentDictionary<string, string> displayNameCorrections = new(StringComparer.OrdinalIgnoreCase);

        private readonly ConcurrentDictionary<string, string[][]> mainQuestLines = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, string[][]> eventQuestLines = new(StringComparer.OrdinalIgnoreCase);

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

            cancellationToken.ThrowIfCancellationRequested();

            foreach (var (k, v) in mainQuestLines)
            {
                exportedAssets.MainQuestLines.TryAdd(k, v);
            }

            cancellationToken.ThrowIfCancellationRequested();

            foreach (var (k, v) in eventQuestLines)
            {
                exportedAssets.EventQuestLines.TryAdd(k, v);
            }

            foreach (var (templateId, recipe) in craftingRecipes)
            {
                cancellationToken.ThrowIfCancellationRequested();

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
            }
        }

        public void AddCraftingRecipe(string name, IReadOnlyDictionary<string, int> ingredients)
        {
            craftingRecipes.TryAdd(name, ingredients);
        }

        public void AddMainQuestLine(string name, string[][] quests)
        {
            mainQuestLines.TryAdd(name, quests);
        }

        public void AddEventQuestLine(string name, string[][] quests)
        {
            eventQuestLines.TryAdd(name, quests);
        }

        public void AddDisplayNameCorrection(string templateId, string correctedName)
        {
            displayNameCorrections.TryAdd(templateId, correctedName);
        }

        public void ApplyDisplayNameCorrections(ExportedAssets exportedAssets)
        {
            foreach (var (templateId, displayName) in displayNameCorrections)
            {
                if (exportedAssets.NamedItems.TryGetValue(templateId, out var item))
                {
                    item.DisplayName = displayName;
                }
            }
        }
    }
}
