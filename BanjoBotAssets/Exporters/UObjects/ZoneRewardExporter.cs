using BanjoBotAssets.Exporters.Helpers;

namespace BanjoBotAssets.Exporters.UObjects
{
    internal sealed class ZoneRewardExporter : UObjectExporter
    {
        public ZoneRewardExporter(IExporterContext services) : base(services) { }

        protected override string Type => "CardPack";

        protected override bool InterestedInAsset(string name) => name.Contains("/ZCP_", StringComparison.OrdinalIgnoreCase);
    }
}
