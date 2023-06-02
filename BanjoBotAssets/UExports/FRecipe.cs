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
using CUE4Parse.UE4.Assets.Utils;

namespace BanjoBotAssets.UExports
{
    [StructFallback]
    public class FRecipe
    {
        public FFortItemQuantityPair[] RecipeResults { get; set; }
#pragma warning disable IDE1006 // Naming Styles
        public bool bIsConsumed { get; set; }
#pragma warning restore IDE1006 // Naming Styles
        public FFortItemQuantityPair[] RecipeCosts { get; set; }
        public FFortItemQuantityPair[] RequiredCatalysts { get; set; }
        public int Score { get; set; }

        public FRecipe(FStructFallback fallback)
        {
            RecipeResults = fallback.GetOrDefault<FFortItemQuantityPair[]>(nameof(RecipeResults));
            bIsConsumed = fallback.GetOrDefault<bool>(nameof(bIsConsumed));
            RecipeCosts = fallback.GetOrDefault<FFortItemQuantityPair[]>(nameof(RecipeCosts));
            RequiredCatalysts = fallback.GetOrDefault<FFortItemQuantityPair[]>(nameof(RequiredCatalysts));
            Score = fallback.GetOrDefault<int>(nameof(Score));
        }
    }
}
