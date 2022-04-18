using CUE4Parse.FN.Exports.FortniteGame.NoProperties;

namespace BanjoBotAssets.Exporters.UObjects
{
    internal sealed class IngredientExporter : UObjectExporter<UFortIngredientItemDefinition>
    {
        public IngredientExporter(IExporterContext services) : base(services) { }

        protected override string Type => "Ingredient";

        protected override bool InterestedInAsset(string name) =>
            name.Contains("Items/Ingredients/Ingredient_", StringComparison.OrdinalIgnoreCase);
    }
}
