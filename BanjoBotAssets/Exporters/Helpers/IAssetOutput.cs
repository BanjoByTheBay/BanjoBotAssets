using BanjoBotAssets.Artifacts;

namespace BanjoBotAssets.Exporters.Helpers
{
    internal interface IAssetOutput
    {
        void AddNamedItem(string name, NamedItemData itemData);

        void AddDifficultyInfo(string name, DifficultyInfo difficultyInfo);

        void AddDefaultItemRatings(ItemRatingTable itemRatings);
        void AddSurvivorItemRatings(ItemRatingTable itemRatings);
        void AddLeadSurvivorItemRatings(ItemRatingTable itemRatings);

        void AddCraftingRecipe(string name, IReadOnlyDictionary<string, int> ingredients);
        void AddDisplayNameCorrection(string schematicTemplateId, string v);

        void AddMainQuestLine(string name, string[][] questPages);
        void AddEventQuestLine(string name, string[][] questPages);

        void CopyTo(ExportedAssets exportedAssets, IList<ExportedRecipe> exportedRecipes, CancellationToken cancellationToken);

        void ApplyDisplayNameCorrections(ExportedAssets exportedAssets);
    }
}
