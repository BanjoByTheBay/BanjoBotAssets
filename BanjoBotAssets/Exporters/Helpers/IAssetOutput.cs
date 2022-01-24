using BanjoBotAssets.Models;

namespace BanjoBotAssets.Exporters.Helpers
{
    internal interface IAssetOutput
    {
        void AddNamedItem(string name, NamedItemData itemData);

        void AddDifficultyInfo(string name, DifficultyInfo difficultyInfo);

        void AddDefaultItemRatings(ItemRatingTable itemRatings);
        void AddSurvivorItemRatings(ItemRatingTable itemRatings);
        void AddLeadSurvivorItemRatings(ItemRatingTable itemRatings);

        void CopyTo(ExportedAssets exportedAssets, IList<ExportedRecipe> exportedRecipes, CancellationToken cancellationToken);

        void AddCraftingRecipe(string name, IReadOnlyDictionary<string, int> ingredients);
    }
}
