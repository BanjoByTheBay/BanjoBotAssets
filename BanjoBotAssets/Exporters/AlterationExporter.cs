using CUE4Parse.FileProvider;
using CUE4Parse.FN.Exports.FortniteGame;

namespace BanjoBotAssets.Exporters
{
    internal sealed class AlterationExporter : UObjectExporter<UFortItemDefinition>
    {
        public AlterationExporter(DefaultFileProvider provider) : base(provider) { }

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
