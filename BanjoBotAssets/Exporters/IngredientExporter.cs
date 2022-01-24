using CUE4Parse.FN.Exports.FortniteGame.NoProperties;

namespace BanjoBotAssets.Exporters
{
    internal sealed class IngredientExporter : UObjectExporter<UFortIngredientItemDefinition>
    {
        public IngredientExporter(AbstractVfsFileProvider provider, ILogger logger) : base(provider, logger) { }

        protected override string Type => "Ingredient";

        protected override bool InterestedInAsset(string name) => name.Contains("Items/Ingredients/Ingredient_");
    }
}
