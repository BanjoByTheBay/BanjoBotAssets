namespace BanjoBotAssets.Exporters
{
    internal sealed class ZoneThemeExporter : BlueprintExporter
    {
        public ZoneThemeExporter(AbstractVfsFileProvider provider, ILogger logger) : base(provider, logger)
        {
        }

        protected override string Type => "ZoneTheme";

        protected override string DisplayNameProperty => "ZoneName";

        protected override string? DescriptionProperty => "ZoneDescription";

        protected override bool InterestedInAsset(string name) => name.Contains("/ZoneThemes/") && name.Contains("/BP_ZT_");
    }
}
