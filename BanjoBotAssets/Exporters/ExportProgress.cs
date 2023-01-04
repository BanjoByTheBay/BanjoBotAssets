namespace BanjoBotAssets.Exporters
{
    internal sealed record ExportProgress
    {
        public int TotalSteps { get; init; }
        public int CompletedSteps { get; init; }
        public required string CurrentItem { get; init; }
        public int AssetsLoaded { get; init; }
        public IEnumerable<string>? FailedAssets { get; init; }
    }
}
