using BanjoBotAssets.Exporters.Blueprints;
using BanjoBotAssets.Exporters.Groups;
using BanjoBotAssets.Exporters.UObjects;

namespace BanjoBotAssets.Config
{
    public class ScopeOptions
    {
        /// <summary>
        /// A comma-separated list of Exporter class names to run. (By default, they all run.)
        /// </summary>
        public string? Only { get; set; }
        /// <summary>
        /// The maximum number of items for a <see cref="BlueprintExporter"/>, <see cref="GroupExporter{TAsset}"/>,
        /// or <see cref="UObjectExporter"/> to export. (By default, they're all exported.)
        /// </summary>
        public int? Limit { get; set; }
        /// <summary>
        /// Whether to keep an existing output file and merge in the newly exported items, rather than overwriting
        /// the file.
        /// </summary>
        public bool Merge { get; set; }
    }
}
