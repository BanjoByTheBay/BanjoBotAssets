using BanjoBotAssets.Exporters.Helpers;

namespace BanjoBotAssets.Exporters.UObjects
{
    internal sealed class TokenExporter : UObjectExporter
    {
        public TokenExporter(IExporterContext services) : base(services)
        {
        }

        protected override string Type => "Token";

        protected override bool InterestedInAsset(string name) => name.Contains("/Items/Tokens/", StringComparison.OrdinalIgnoreCase);
    }
}
