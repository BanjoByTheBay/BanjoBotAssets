using BanjoBotAssets.Artifacts.Models;
using BanjoBotAssets.Exporters.Helpers;

namespace BanjoBotAssets.Exporters.UObjects
{
    internal sealed class AlterationExporter : UObjectExporter<UFortItemDefinition>
    {
        public AlterationExporter(IExporterContext services) : base(services) { }

        protected override string Type => "Alteration";

        protected override bool InterestedInAsset(string name) =>
            name.Contains("/AID_", StringComparison.OrdinalIgnoreCase) &&
            (name.Contains("/Alteration_v2/", StringComparison.OrdinalIgnoreCase) || name.Contains("/Defenders/", StringComparison.OrdinalIgnoreCase));

        protected override Task<bool> ExportAssetAsync(UFortItemDefinition asset, NamedItemData namedItemData, Dictionary<ImageType, string> imagePaths)
        {
            namedItemData.DisplayName = asset.Description?.Text ?? $"<Alteration:{asset.Name}>";
            namedItemData.Description = null;
            return Task.FromResult(true);
        }
    }
}
