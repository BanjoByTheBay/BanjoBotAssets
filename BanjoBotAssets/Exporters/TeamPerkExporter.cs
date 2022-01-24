namespace BanjoBotAssets.Exporters
{
    internal sealed class TeamPerkExporter : UObjectExporter
    {
        public TeamPerkExporter(AbstractVfsFileProvider provider, ILogger logger) : base(provider, logger) { }

        protected override string Type => "TeamPerk";

        protected override bool InterestedInAsset(string name) => name.Contains("/TPID_");

        protected override async Task<bool> ExportAssetAsync(UObject asset, NamedItemData namedItemData)
        {
            Interlocked.Increment(ref assetsLoaded);
            var grantedAbilityKit = await asset.GetOrDefault<FSoftObjectPath>("GrantedAbilityKit").LoadAsync(provider);
            namedItemData.Description = await AbilityDescription.GetAsync(grantedAbilityKit, this) ?? $"<{Resources.Field_NoDescription}>";
            return true;
        }
    }
}
