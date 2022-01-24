namespace BanjoBotAssets.Exporters
{
    internal sealed class WorldItemExporter : UObjectExporter
    {
        public WorldItemExporter(AbstractVfsFileProvider provider, ILogger logger) : base(provider, logger)
        {
        }

        protected override string Type => "WorldItem";

        protected override bool InterestedInAsset(string name) =>
             name.Contains("Items/ResourcePickups/") && !name.Contains("/Athena");
    }
}
