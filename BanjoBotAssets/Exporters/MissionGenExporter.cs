namespace BanjoBotAssets.Exporters
{
    internal sealed class MissionGenExporter : BlueprintExporter
    {
        public MissionGenExporter(DefaultFileProvider provider) : base(provider) { }

        protected override string Type => "MissionGen";

        protected override string DisplayNameProperty => "MissionName";

        protected override string? DescriptionProperty => "MissionDescription";

        protected override bool InterestedInAsset(string name) => name.Contains("/MissionGens/") && name.Contains("/World/");
    }
}
