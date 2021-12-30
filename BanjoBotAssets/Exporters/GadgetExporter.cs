using CUE4Parse.FileProvider;
using CUE4Parse.FN.Exports.FortniteGame;
using CUE4Parse.UE4.Assets.Exports;

namespace BanjoBotAssets.Exporters
{
    internal sealed class GadgetExporter : UObjectExporter<UFortGadgetItemDefinition>
    {
        public GadgetExporter(DefaultFileProvider provider) : base(provider) { }

        protected override string Type => "Gadget";

        protected override bool InterestedInAsset(string name) => name.Contains("/Gadgets/") && name.Contains("/G_");

        protected override async Task<bool> ExportAssetAsync(UFortGadgetItemDefinition asset, NamedItemData namedItemData)
        {
            if (asset.GameplayAbility.AssetPathName.IsNone)
            {
                Console.WriteLine("Skipping gadget with no gameplay ability: {0}", asset.Name);
                return false;
            }

            Interlocked.Increment(ref assetsLoaded);
            var gameplayAbility = await asset.GameplayAbility.LoadAsync(provider);
            namedItemData.Description = await AbilityDescription.GetAsync(gameplayAbility, this);
            return true;
        }
    }
}
