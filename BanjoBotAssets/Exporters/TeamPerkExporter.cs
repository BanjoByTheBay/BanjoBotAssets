using CUE4Parse.FileProvider;
using CUE4Parse.FN.Exports.FortniteGame.NoProperties;
using CUE4Parse.UE4.Objects.UObject;

namespace BanjoBotAssets.Exporters
{
    internal sealed class TeamPerkExporter : UObjectExporter<UFortIngredientItemDefinition>
    {
        public TeamPerkExporter(DefaultFileProvider provider) : base(provider) { }

        protected override string Type => "TeamPerk";

        protected override bool InterestedInAsset(string name) => name.Contains("/TPID_");

        protected override async Task<bool> ExportAssetAsync(UFortIngredientItemDefinition asset, NamedItemData namedItemData)
        {
            Interlocked.Increment(ref assetsLoaded);
            var grantedAbilityKit = await asset.GetOrDefault<FSoftObjectPath>("GrantedAbilityKit").LoadAsync(provider);
            namedItemData.Description = await AbilityDescription.GetAsync(grantedAbilityKit, this) ?? "<No description>";
            return true;
        }
    }
}
