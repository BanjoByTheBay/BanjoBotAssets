namespace BanjoBotAssets.Config
{
    public class ExportedFileOptions
    {
        /// <summary>
        /// The path of the output file.
        /// </summary>
        public string Path { get; set; } = null!;
        /// <summary>
        /// Whether to keep an existing output file and merge in the newly exported items,
        /// rather than overwriting the file.
        /// </summary>
        public bool Merge { get; set; }
    }
}