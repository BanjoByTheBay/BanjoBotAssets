using BanjoBotAssets.Artifacts.Models;

namespace BanjoBotAssets.Exporters.UObjects
{
    internal sealed class TeamPerkExporter : UObjectExporter
    {
        public TeamPerkExporter(IExporterContext services) : base(services) { }

        protected override string Type => "TeamPerk";

        protected override bool InterestedInAsset(string name) => name.Contains("/TPID_", StringComparison.OrdinalIgnoreCase);

        protected override async Task<bool> ExportAssetAsync(UObject asset, NamedItemData namedItemData, Dictionary<ImageType, string> imagePaths)
        {
            Interlocked.Increment(ref assetsLoaded);
            var grantedAbilityKit = await asset.GetOrDefault<FSoftObjectPath>("GrantedAbilityKit").LoadAsync(provider);
            namedItemData.Description = await abilityDescription.GetAsync(grantedAbilityKit, this) ?? $"<{Resources.Field_NoDescription}>";

            if (grantedAbilityKit.GetResourceObjectPath("IconBrush") is string path)
            {
                imagePaths.Add(ImageType.Icon, path);
            }

            return true;
        }
    }
}
