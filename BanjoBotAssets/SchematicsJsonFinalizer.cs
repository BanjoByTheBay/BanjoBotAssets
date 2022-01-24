using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace BanjoBotAssets
{
    internal class SchematicsJsonFinalizer : IExportFinalizer
    {
        private readonly IOptions<ExportedFileOptions<SchematicsJsonFinalizer>> options;

        public SchematicsJsonFinalizer(IOptions<ExportedFileOptions<SchematicsJsonFinalizer>> options)
        {
            this.options = options;
        }

        public Task RunAsync(ExportedAssets exportedAssets, IList<ExportedRecipe> exportedRecipes, CancellationToken cancellationToken = default)
        {
            var recipesToExclude = new Stack<int>();

            var schematicSubTypeToRecipeType = new Dictionary<string, string>
            {
                [Resources.Field_Schematic_Ceiling] = Resources.Field_Recipe_Trap,
                [Resources.Field_Schematic_Floor] = Resources.Field_Recipe_Trap,
                [Resources.Field_Schematic_Wall] = Resources.Field_Recipe_Trap,

                [Resources.Field_Schematic_Axe] = Resources.Field_Recipe_Melee,
                [Resources.Field_Schematic_Club] = Resources.Field_Recipe_Melee,
                [Resources.Field_Schematic_Hardware] = Resources.Field_Recipe_Melee,
                [Resources.Field_Schematic_Scythe] = Resources.Field_Recipe_Melee,
                [Resources.Field_Schematic_Spear] = Resources.Field_Recipe_Melee,
                [Resources.Field_Schematic_Sword] = Resources.Field_Schematic_Sword,

                [Resources.Field_Schematic_Assault] = Resources.Field_Recipe_Ranged,
                [Resources.Field_Schematic_Explosive] = Resources.Field_Recipe_Ranged,
                [Resources.Field_Schematic_Pistol] = Resources.Field_Recipe_Ranged,
                [Resources.Field_Schematic_Shotgun] = Resources.Field_Recipe_Ranged,
                [Resources.Field_Schematic_SMG] = Resources.Field_Recipe_Ranged,
                [Resources.Field_Schematic_Sniper] = Resources.Field_Recipe_Ranged,
            };

            cancellationToken.ThrowIfCancellationRequested();

            for (int i = 0; i < exportedRecipes.Count; i++)
            {
                var recipe = exportedRecipes[i];

                if (recipe.ItemName == null)
                {
                    recipesToExclude.Push(i);
                    continue;
                }

                // change schematic ID to display name and fill in other fields
                if (!exportedAssets.NamedItems.TryGetValue(recipe.ItemName, out var schematic))
                {
                    Console.WriteLine(Resources.Warning_UnmatchedCraftingRecipe, recipe.ItemName);
                    recipesToExclude.Push(i);
                    continue;
                }

                recipe.ItemName = schematic.DisplayName ?? "";
                recipe.Type = schematic.SubType != null ? schematicSubTypeToRecipeType.GetValueOrDefault(schematic.SubType, "") : "";
                recipe.Subtype = schematic.SubType ?? "";
                recipe.Rarity = schematic.Rarity ?? "";
                recipe.Tier = schematic.Tier ?? 0;

                if (schematic is SchematicItemData { EvoType: string evoType })
                {
                    recipe.Material = evoType.ToLower(CultureInfo.InvariantCulture) switch
                    {
                        "ore" => Resources.Field_Schematic_Ore,
                        "crystal" => Resources.Field_Schematic_Crystal,
                        _ => $"<{evoType}>",
                    };
                }

                // change ingredient IDs to display names
                if (recipe.Ingredient1 != null)
                    recipe.Ingredient1 = exportedAssets.NamedItems.GetValueOrDefault(recipe.Ingredient1)?.DisplayName;
                if (recipe.Ingredient2 != null)
                    recipe.Ingredient2 = exportedAssets.NamedItems.GetValueOrDefault(recipe.Ingredient2)?.DisplayName;
                if (recipe.Ingredient3 != null)
                    recipe.Ingredient3 = exportedAssets.NamedItems.GetValueOrDefault(recipe.Ingredient3)?.DisplayName;
                if (recipe.Ingredient4 != null)
                    recipe.Ingredient4 = exportedAssets.NamedItems.GetValueOrDefault(recipe.Ingredient4)?.DisplayName;
                if (recipe.Ingredient5 != null)
                    recipe.Ingredient5 = exportedAssets.NamedItems.GetValueOrDefault(recipe.Ingredient5)?.DisplayName;

                cancellationToken.ThrowIfCancellationRequested();
            }

            while (recipesToExclude.Count > 0)
                exportedRecipes.RemoveAt(recipesToExclude.Pop());

            using (var file = File.CreateText(Resources.File_schematics_json))
            {
                var settings = new JsonSerializerSettings { ContractResolver = NullToEmptyStringResolver.Instance, Formatting = Formatting.Indented };
                var serializer = JsonSerializer.CreateDefault(settings);
                serializer.Serialize(file, exportedRecipes);
            }

            return Task.CompletedTask;
        }
    }
}
