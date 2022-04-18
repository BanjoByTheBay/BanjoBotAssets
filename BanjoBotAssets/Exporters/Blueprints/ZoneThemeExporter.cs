namespace BanjoBotAssets.Exporters.Blueprints
{
    internal sealed class ZoneThemeExporter : BlueprintExporter
    {
        public ZoneThemeExporter(IExporterContext services) : base(services) { }

        protected override string Type => "ZoneTheme";

        protected override string DisplayNameProperty => "ZoneName";

        protected override string? DescriptionProperty => "ZoneDescription";

        protected override bool InterestedInAsset(string name) =>
            name.Contains("/ZoneThemes/", StringComparison.OrdinalIgnoreCase) && name.Contains("/BP_ZT_", StringComparison.OrdinalIgnoreCase);
    }
}
