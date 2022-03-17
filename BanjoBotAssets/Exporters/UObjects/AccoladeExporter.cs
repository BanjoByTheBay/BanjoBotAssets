using BanjoBotAssets.Exporters.Helpers;

// TODO: include accolade XP amounts (XpRewardAmount property -> STWAccoladeXP curve table)

namespace BanjoBotAssets.Exporters.UObjects
{
    internal sealed class AccoladeExporter : UObjectExporter
    {
        public AccoladeExporter(IExporterContext services) : base(services) { }

        protected override string Type => "Accolades";

        protected override bool InterestedInAsset(string name) => name.Contains("/AccoladeId_STW_", StringComparison.OrdinalIgnoreCase);
    }
}
