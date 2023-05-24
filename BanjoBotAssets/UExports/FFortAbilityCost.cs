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
using CUE4Parse.FN.Structs.GA;
using CUE4Parse.UE4.Assets.Utils;

namespace BanjoBotAssets.UExports
{
    [StructFallback]
    public class FFortAbilityCost
    {
        public EFortAbilityCostSource? CostSource { get; set; }
        public FScalableFloat? CostValue { get; set; }
        public FPackageIndex? ItemDefinition { get; set; }
#pragma warning disable IDE1006 // Naming Styles
        public bool bOnlyApplyCostOnHit { get; set; }
#pragma warning restore IDE1006 // Naming Styles

        public FFortAbilityCost(FStructFallback fallback)
        {
            CostSource = fallback.GetOrDefault<EFortAbilityCostSource>(nameof(CostSource));
            CostValue = fallback.GetOrDefault<FScalableFloat>(nameof(CostValue));
            ItemDefinition = fallback.GetOrDefault<FPackageIndex>(nameof(ItemDefinition));
            bOnlyApplyCostOnHit = fallback.GetOrDefault<bool>(nameof(bOnlyApplyCostOnHit));
        }
    }
}
