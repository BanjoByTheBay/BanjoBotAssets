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
    /// <summary>
    /// Like <see cref="FGameplayEffectApplicationInfo"/>, but the reference to the <see cref="GameplayEffect"/>
    /// is a <see cref="FPackageIndex"/> instead of a <see cref="FSoftObjectPath"/>.
    /// </summary>
    [StructFallback]
    public class FGameplayEffectApplicationInfoHard
    {
        public FPackageIndex GameplayEffect { get; set; }
        public float Level { get; set; }

        public FGameplayEffectApplicationInfoHard(FStructFallback fallback)
        {
            GameplayEffect = fallback.GetOrDefault<FPackageIndex>(nameof(GameplayEffect));
            Level = fallback.GetOrDefault<float>(nameof(Level));
        }
    }
}
