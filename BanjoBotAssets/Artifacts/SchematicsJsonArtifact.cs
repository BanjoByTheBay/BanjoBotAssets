/* Copyright 2023 Tara "Dino" Cassatt
 * 
 * This file is part of BanjoBotAssets.
 * 
 * BanjoBotAssets is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * BanjoBotAssets is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with BanjoBotAssets.  If not, see <http://www.gnu.org/licenses/>.
 */
using BanjoBotAssets.Config;
using Newtonsoft.Json;

namespace BanjoBotAssets.Artifacts
{
    internal sealed class SchematicsJsonArtifact(
        ExportedFileOptions options,
        ILogger<SchematicsJsonArtifact> logger) : IExportArtifact
    {
        public async Task RunAsync(ExportedAssets exportedAssets, IList<ExportedRecipe> exportedRecipes, CancellationToken cancellationToken = default)
        {
            var recipesToExclude = new Stack<int>();

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
                if (!exportedAssets.NamedItems.TryGetValue(recipe.ItemName, out var itemData) || itemData is not SchematicItemData schematic)
                {
                    logger.LogDebug(Resources.Warning_UnmatchedCraftingRecipe, recipe.ItemName);
                    recipesToExclude.Push(i);
                    continue;
                }

                recipe.ItemName = schematic.DisplayName ?? "";
                recipe.Type = schematic.Category ?? "";
                recipe.Subtype = schematic.SubType ?? "";
                recipe.Rarity = schematic.Rarity ?? "";
                recipe.Tier = schematic.Tier ?? 0;

                if (schematic is SchematicItemData { EvoType: string evoType })
                {
                    recipe.Material = evoType.ToLower(CultureInfo.InvariantCulture) switch
                    {
                        "ore" => Resources.Field_Schematic_Ore,
                        "crystal" => Resources.Field_Schematic_Crystal,
                        "" => "",
                        _ => $"<{evoType}>",
                    };
                }

                // change ingredient IDs to display names
                if (recipe.Ingredient1 != null && exportedAssets.NamedItems.TryGetValue(recipe.Ingredient1, out var item1))
                    recipe.Ingredient1 = item1.DisplayName;
                if (recipe.Ingredient2 != null && exportedAssets.NamedItems.TryGetValue(recipe.Ingredient2, out var item2))
                    recipe.Ingredient2 = item2.DisplayName;
                if (recipe.Ingredient3 != null && exportedAssets.NamedItems.TryGetValue(recipe.Ingredient3, out var item3))
                    recipe.Ingredient3 = item3.DisplayName;
                if (recipe.Ingredient4 != null && exportedAssets.NamedItems.TryGetValue(recipe.Ingredient4, out var item4))
                    recipe.Ingredient4 = item4.DisplayName;
                if (recipe.Ingredient5 != null && exportedAssets.NamedItems.TryGetValue(recipe.Ingredient5, out var item5))
                    recipe.Ingredient5 = item5.DisplayName;

                cancellationToken.ThrowIfCancellationRequested();
            }

            while (recipesToExclude.Count > 0)
                exportedRecipes.RemoveAt(recipesToExclude.Pop());

            var serializer = ExportedRecipes.CreateJsonSerializer();

            string path = options.Path;

            if (options.Merge && File.Exists(path))
            {
                logger.LogInformation(Resources.Status_MergingIntoExistingArtifact, path);

                IList<ExportedRecipe> previous;
                using (var stream = File.OpenText(path))
                {
                    var rdr = new JsonTextReader(stream);
#pragma warning disable CA1863 // Use 'CompositeFormat'
                    previous = serializer.Deserialize<IList<ExportedRecipe>>(rdr)
                        ?? throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.FormatString_Error_CannotReadPreviousArtifact, path));
#pragma warning restore CA1863 // Use 'CompositeFormat'
                }

                ExportedRecipes.Merge(previous, exportedRecipes);
                exportedRecipes = previous;
            }
            else
            {
                logger.LogInformation(Resources.Status_WritingFreshArtifact, path);
            }

            exportedRecipes = [.. exportedRecipes
                .OrderBy(r => r.ItemName)
                .ThenBy(r => r.Type)
                .ThenBy(r => r.Subtype)
                .ThenBy(r => r.Rarity)
                .ThenBy(r => r.Tier)
                .ThenBy(r => r.Material)];

            await using var file = File.CreateText(path);
            serializer.Serialize(file, exportedRecipes);
        }
    }
}
