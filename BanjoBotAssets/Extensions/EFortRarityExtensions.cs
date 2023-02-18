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
namespace BanjoBotAssets.Extensions
{
    internal static class EFortRarityExtensions
    {
        public static string ToShortString(this EFortRarity rarity) => rarity switch
        {
            EFortRarity.Common => "C",
            EFortRarity.Uncommon => "UC",
            EFortRarity.Rare => "R",
            EFortRarity.Epic => "VR",
            EFortRarity.Legendary => "SR",
            EFortRarity.Mythic => "UR",
            _ => throw new ArgumentOutOfRangeException(nameof(rarity)),
        };
    }
}
