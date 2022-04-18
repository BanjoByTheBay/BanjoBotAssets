namespace BanjoBotAssets.Config
{
    public class ImageExportOptions
    {
        public Dictionary<ImageType, WantImageExport> Type { get; set; } = null!;
        public string OutputDirectory { get; set; } = null!;
    }

    public enum WantImageExport
    {
        No,
        PathOnly,
        Yes,
    }
}
