using CUE4Parse.FileProvider;

namespace BanjoBotAssets.Exporters
{
    internal sealed class AccountResourceExporter : UObjectExporter
    {
        public AccountResourceExporter(DefaultFileProvider provider) : base(provider) { }

        protected override string Type => "AccountResource";

        protected override bool InterestedInAsset(string name) => name.Contains("/PersistentResources/");
    }
}
