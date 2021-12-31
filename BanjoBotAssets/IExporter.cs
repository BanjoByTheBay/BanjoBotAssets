namespace BanjoBotAssets
{
    internal struct ExportProgress
    {
        public int TotalSteps { get; set; }
        public int CompletedSteps { get; set; }
        public string CurrentItem { get; set; }
        public int AssetsLoaded { get; set; }
    }

    internal interface IAssetCounter
    {
        void CountAssetLoaded();
    }

    internal interface IExporter : IAssetCounter
    {
        void ObserveAsset(string name);

        Task ExportAssetsAsync(IProgress<ExportProgress> progress, IAssetOutput output);

        int AssetsLoaded { get; }
    }

    internal interface IAssetOutput
    {
        void AddNamedItem(string name, NamedItemData itemData);

        void AddDifficultyInfo(string name, DifficultyInfo difficultyInfo);

        void AddDefaultItemRatings(ItemRatingTable itemRatings);
        void AddSurvivorItemRatings(ItemRatingTable itemRatings);
        void AddLeadSurvivorItemRatings(ItemRatingTable itemRatings);

        void CopyTo(ExportedAssets exportedAssets, IList<ExportedRecipe> exportedRecipes);

        void AddCraftingRecipe(string name, IReadOnlyDictionary<string, int> ingredients);
    }
}
