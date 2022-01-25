using BanjoBotAssets.Artifacts;
using BanjoBotAssets.Exporters.Helpers;

namespace BanjoBotAssets.Exporters.UObjects
{
    internal sealed class AlterationExporter : UObjectExporter<UFortItemDefinition>
    {
        public AlterationExporter(IExporterContext services) : base(services) { }

        protected override string Type => "Alteration";

        protected override bool InterestedInAsset(string name) => name.Contains("/AID_") && (name.Contains("/Alteration_v2/") || name.Contains("/Defenders/"));

        protected override Task<bool> ExportAssetAsync(UFortItemDefinition asset, NamedItemData namedItemData)
        {
            namedItemData.DisplayName = asset.Description?.Text ?? $"<Alteration:{asset.Name}>";
            namedItemData.Description = null;
            return Task.FromResult(true);
        }
    }
}
