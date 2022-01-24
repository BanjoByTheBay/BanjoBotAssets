using BanjoBotAssets.Exporters.Impl;

namespace BanjoBotAssets.Exporters
{
    internal sealed class WorldItemExporter : UObjectExporter
    {
        public WorldItemExporter(IExporterContext services) : base(services) { }

        protected override string Type => "WorldItem";

        protected override bool InterestedInAsset(string name) =>
             name.Contains("Items/ResourcePickups/") && !name.Contains("/Athena");
    }
}
