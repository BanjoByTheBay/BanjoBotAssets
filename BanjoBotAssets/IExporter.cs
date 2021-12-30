namespace BanjoBotAssets
{
    internal struct ExportProgress
    {
        public int TotalSteps { get; set; }
        public int CompletedSteps { get; set; }
        public string CurrentItem { get; set; }
        public int AssetsLoaded { get; set; }
    }

    internal interface IExporter : IAssetCounter
    {
        void ObserveAsset(string name);

        Task ExportAssetsAsync(IProgress<ExportProgress> progress, ExportedAssets output);

        int AssetsLoaded { get; }
    }

    internal interface IAssetCounter
    {
        void CountAssetLoaded();
    }
}
