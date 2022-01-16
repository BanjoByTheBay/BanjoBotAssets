namespace BanjoBotAssets.Exporters
{
    internal sealed class GameplayModifierExporter : UObjectExporter<UFortItemDefinition>
    {
        public GameplayModifierExporter(DefaultFileProvider provider) : base(provider) { }

        protected override string Type => "GameplayModifier";

        protected override bool InterestedInAsset(string name) =>
            name.Contains("/GM_") || name.Contains("/GMID_") || name.Contains("/UnlockModifiers/");
    }
}
