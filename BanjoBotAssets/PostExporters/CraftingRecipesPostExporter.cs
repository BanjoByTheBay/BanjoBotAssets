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

namespace BanjoBotAssets.PostExporters
{
    internal sealed class CraftingRecipesPostExporter : IPostExporter
    {
        public int AssetsLoaded => 0;

        public void CountAssetLoaded()
        {
            throw new NotSupportedException();
        }

        public Task ProcessExportsAsync(ExportedAssets exportedAssets, IList<ExportedRecipe> exportedRecipes, CancellationToken cancellationToken = default)
        {
            /**
             * NOTE: we store template IDs instead of display names in the recipes here.
             * they're replaced with display names in <see cref="Artifacts.SchematicsJsonArtifact"/>.
             */

            foreach (var i in exportedAssets.NamedItems.Values.OfType<SchematicItemData>())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (i.CraftingCost != null)
                {
                    var ingredients = new Queue<KeyValuePair<string, int>>(i.CraftingCost);

                    var exportedRecipe = new ExportedRecipe { ItemName = $"{i.Type}:{i.Name}" };

                    if (ingredients.TryDequeue(out var pair))
                        (exportedRecipe.Ingredient1, exportedRecipe.Quantity1!) = pair;
                    if (ingredients.TryDequeue(out pair))
                        (exportedRecipe.Ingredient2, exportedRecipe.Quantity2!) = pair;
                    if (ingredients.TryDequeue(out pair))
                        (exportedRecipe.Ingredient3, exportedRecipe.Quantity3!) = pair;
                    if (ingredients.TryDequeue(out pair))
                        (exportedRecipe.Ingredient4, exportedRecipe.Quantity4!) = pair;
                    if (ingredients.TryDequeue(out pair))
                        (exportedRecipe.Ingredient5, exportedRecipe.Quantity5!) = pair;

                    exportedRecipes.Add(exportedRecipe);
                }
            }

            return Task.CompletedTask;
        }
    }
}
