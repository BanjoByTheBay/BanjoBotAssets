using CUE4Parse.FileProvider;

namespace BanjoBotAssets.Exporters
{
    internal sealed class ZoneRewardExporter : UObjectExporter
    {
        public ZoneRewardExporter(DefaultFileProvider provider) : base(provider) { }

        protected override string Type => "CardPack";

        protected override bool InterestedInAsset(string name) => name.Contains("/ZCP_");
    }
}
