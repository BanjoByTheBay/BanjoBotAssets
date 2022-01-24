using BanjoBotAssets.Exporters.Impl;

namespace BanjoBotAssets.Exporters
{
    internal sealed class AccountResourceExporter : UObjectExporter
    {
        public AccountResourceExporter(IExporterContext services) : base(services) { }

        protected override string Type => "AccountResource";

        protected override bool InterestedInAsset(string name) => name.Contains("/PersistentResources/");
    }
}
