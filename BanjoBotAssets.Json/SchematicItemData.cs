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
using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;

namespace BanjoBotAssets.Json
{
    [NamedItemData("Schematic")]
    public sealed class SchematicItemData : NamedItemData
    {
        public string? Category { get; set; }
        public string? EvoType { get; set; }
        [DisallowNull]
        public AlterationSlot[]? AlterationSlots { get; set; }
        public string? TriggerType { get; set; }
        public string? DisplayTier { get; set; }

        public RangedWeaponStats? RangedWeaponStats { get; set; }
        public MeleeWeaponStats? MeleeWeaponStats { get; set; }
        public TrapStats? TrapStats { get; set; }

        [DisallowNull]
        public Dictionary<string, int>? CraftingCost { get; set; }
        [DisallowNull]
        public string? CraftingResult { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? CraftingAmount { get; set; }

        public ItemRecipe? AlternateTierUpRecipe { get; set; }
    }
}
