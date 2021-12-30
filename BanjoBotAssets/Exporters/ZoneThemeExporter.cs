using CUE4Parse.FileProvider;

namespace BanjoBotAssets.Exporters
{
    internal sealed class ZoneThemeExporter : BlueprintExporter
    {
        public ZoneThemeExporter(DefaultFileProvider provider) : base(provider) { }

        protected override string Type => "ZoneTheme";

        protected override string DisplayNameProperty => "ZoneName";

        protected override string? DescriptionProperty => "ZoneDescription";

        protected override bool InterestedInAsset(string name) => name.Contains("/ZoneThemes/") && name.Contains("/BP_ZT_");
    }
}
