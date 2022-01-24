using BanjoBotAssets.Exporters.Helpers;

namespace BanjoBotAssets.Exporters.UObjects
{
    internal sealed class GameplayModifierExporter : UObjectExporter<UFortItemDefinition>
    {
        public GameplayModifierExporter(IExporterContext services) : base(services) { }

        protected override string Type => "GameplayModifier";

        protected override bool InterestedInAsset(string name) =>
            name.Contains("/GM_") || name.Contains("/GMID_") || name.Contains("/UnlockModifiers/");
    }
}
