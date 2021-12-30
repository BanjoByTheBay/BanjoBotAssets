namespace BanjoBotAssets.Exporters
{
    internal sealed class TeamPerkExporter : UObjectExporter
    {
        public TeamPerkExporter(DefaultFileProvider provider) : base(provider) { }

        protected override string Type => "TeamPerk";

        protected override bool InterestedInAsset(string name) => name.Contains("/TPID_");

        protected override async Task<bool> ExportAssetAsync(UObject asset, NamedItemData namedItemData)
        {
            Interlocked.Increment(ref assetsLoaded);
            var grantedAbilityKit = await asset.GetOrDefault<FSoftObjectPath>("GrantedAbilityKit").LoadAsync(provider);
            namedItemData.Description = await AbilityDescription.GetAsync(grantedAbilityKit, this) ?? "<No description>";
            return true;
        }
    }
}
