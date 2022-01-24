namespace BanjoBotAssets.Exporters
{
    internal sealed class AccountResourceExporter : UObjectExporter
    {
        public AccountResourceExporter(AbstractVfsFileProvider provider, ILogger logger) : base(provider, logger) { }

        protected override string Type => "AccountResource";

        protected override bool InterestedInAsset(string name) => name.Contains("/PersistentResources/");
    }
}
