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
using CUE4Parse.UE4.Objects.GameplayTags;

namespace BanjoBotAssets.UExports
{
    [StructFallback]
    public class FInheritedTagContainer
    {
        public FGameplayTagContainer CombinedTags { get; set; }
        public FGameplayTagContainer Added { get; set; }
        public FGameplayTagContainer Removed { get; set; }

        public FInheritedTagContainer(FStructFallback fallback)
        {
            CombinedTags = fallback.GetOrDefault<FGameplayTagContainer>(nameof(CombinedTags));
            Added = fallback.GetOrDefault<FGameplayTagContainer>(nameof(Added));
            Removed = fallback.GetOrDefault<FGameplayTagContainer>(nameof(Removed));
        }
    }
}
