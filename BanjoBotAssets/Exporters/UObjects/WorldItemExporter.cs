namespace BanjoBotAssets.Exporters.UObjects
{
    internal sealed class WorldItemExporter : UObjectExporter
    {
        public WorldItemExporter(IExporterContext services) : base(services) { }

        protected override string Type => "WorldItem";

        protected override bool InterestedInAsset(string name) =>
             name.Contains("Items/ResourcePickups/", StringComparison.OrdinalIgnoreCase) && !name.Contains("/Athena", StringComparison.OrdinalIgnoreCase);
    }
}
