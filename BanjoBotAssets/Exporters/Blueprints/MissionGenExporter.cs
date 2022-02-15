using BanjoBotAssets.Exporters.Helpers;

namespace BanjoBotAssets.Exporters.Blueprints
{
    internal sealed class MissionGenExporter : BlueprintExporter
    {
        public MissionGenExporter(IExporterContext services) : base(services) { }

        protected override string Type => "MissionGen";

        protected override string DisplayNameProperty => "MissionName";

        protected override string? DescriptionProperty => "MissionDescription";

        protected override bool InterestedInAsset(string name) =>
            name.Contains("/MissionGens/", StringComparison.OrdinalIgnoreCase) && name.Contains("/World/", StringComparison.OrdinalIgnoreCase);
    }
}
