namespace BanjoBotAssets.Exporters
{
    internal sealed class ZoneRewardExporter : UObjectExporter
    {
        public ZoneRewardExporter(AbstractVfsFileProvider provider, ILogger logger) : base(provider, logger)
        {
        }

        protected override string Type => "CardPack";

        protected override bool InterestedInAsset(string name) => name.Contains("/ZCP_");
    }
}
