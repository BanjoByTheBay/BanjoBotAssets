using BanjoBotAssets.Artifacts.Models;
using BanjoBotAssets.Exporters.Helpers;

namespace BanjoBotAssets.Exporters.UObjects
{
    internal sealed class GadgetExporter : UObjectExporter<UFortGadgetItemDefinition>
    {
        public GadgetExporter(IExporterContext services) : base(services) { }

        protected override string Type => "Gadget";

        protected override bool InterestedInAsset(string name) =>
            name.Contains("/Gadgets/", StringComparison.OrdinalIgnoreCase) && name.Contains("/G_", StringComparison.OrdinalIgnoreCase);

        protected override async Task<bool> ExportAssetAsync(UFortGadgetItemDefinition asset, NamedItemData namedItemData)
        {
            if (asset.GameplayAbility.AssetPathName.IsNone)
            {
                logger.LogInformation(Resources.Status_SkippingGadgetWithoutAbility, asset.Name);
                return false;
            }

            Interlocked.Increment(ref assetsLoaded);
            var gameplayAbility = await asset.GameplayAbility.LoadAsync(provider);
            namedItemData.Description = await abilityDescription.GetAsync(gameplayAbility, this);
            return true;
        }
    }
}
