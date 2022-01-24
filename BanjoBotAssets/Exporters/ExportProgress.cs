namespace BanjoBotAssets.Exporters
{
    internal struct ExportProgress
    {
        public int TotalSteps { get; set; }
        public int CompletedSteps { get; set; }
        public string CurrentItem { get; set; }
        public int AssetsLoaded { get; set; }
    }
}
