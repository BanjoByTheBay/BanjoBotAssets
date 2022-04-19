// TODO: export LevelData and DisplayType

namespace BanjoBotAssets.Exporters.UObjects
{
    internal class HomebaseNodeExporter : UObjectExporter<UFortHomebaseNodeItemDefinition>
    {
        public HomebaseNodeExporter(IExporterContext services) : base(services)
        {
        }

        protected override string Type => "HomebaseNode";

        protected override bool InterestedInAsset(string name) =>
            name.Contains("/Items/HomebaseNodes/", StringComparison.OrdinalIgnoreCase);
    }
}
