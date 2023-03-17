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
using System.Diagnostics.CodeAnalysis;

namespace BanjoBotAssets.Json
{
    public sealed class ExportedRecipe
    {
        [DisallowNull]
        public string? ItemName { get; set; }
        /// <summary>
        /// "Ranged", "Melee", or "Trap"
        /// </summary>
        /// <remarks>
        /// Localized using Resources.Field_Recipe_*
        /// </remarks>
        [DisallowNull]
        public string? Type { get; set; }
        /// <summary>
        /// "Assault", "Axe", "Ceiling", "Club", "Explosive", "Floor", "Hardware",
        /// "Pistol", "Scythe", "Shotgun", "SMG", "Sniper", "Spear", "Sword", or "Wall"
        /// </summary>
        /// <remarks>
        /// Localized using Resources.Field_Schematic_*
        /// </remarks>
        [DisallowNull]
        public string? Subtype { get; set; }
        public int Tier { get; set; }
        /// <summary>
        /// "Ore", "Crystal", or ""
        /// </summary>
        public string? Material { get; set; }
        /// <summary>
        /// "Common", "Uncommon", "Rare", "Epic", "Legendary", or "Mythic"
        /// </summary>
        /// <remarks>
        /// Localized using ELanguage (?)
        /// </remarks>
        [DisallowNull]
        public string? Rarity { get; set; }
        public string? Ingredient1 { get; set; }
        public object? Quantity1 { get; set; }
        public string? Ingredient2 { get; set; }
        public object? Quantity2 { get; set; }
        public string? Ingredient3 { get; set; }
        public object? Quantity3 { get; set; }
        public string? Ingredient4 { get; set; }
        public object? Quantity4 { get; set; }
        public string? Ingredient5 { get; set; }
        public object? Quantity5 { get; set; }

        public string GetMergeKey() => $"{ItemName}|{Tier}|{Material}|{Rarity}";
    }
}
