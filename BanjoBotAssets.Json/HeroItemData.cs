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
    [NamedItemData("Hero")]
    public sealed class HeroItemData : NamedItemData
    {
        public string? HeroPerkName { get; set; }
        [DisallowNull]
        public string? HeroPerk { get; set; }
        [DisallowNull]
        public string? HeroPerkDescription { get; set; }
        public string? CommanderPerkName { get; set; }
        [DisallowNull]
        public string? CommanderPerk { get; set; }
        [DisallowNull]
        public string? CommanderPerkDescription { get; set; }
        public string? UnlocksTeamPerk { get; set; }
        public PerkRequirement? HeroPerkRequirement { get; set; }

        [DisallowNull]
        public string[]? HeroAbilities { get; set; }
    }

    /// <summary>
    /// Represents a requirement for a perk to be activated in a hero loadout.
    /// </summary>
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public sealed class PerkRequirement
    {
        public string Description { get; set; } = "";
        /// <summary>
        /// One or more tags indicating granted abilities that must be present on the commander for this perk to be active.
        /// </summary>
        public string[]? CommanderTag { get; set; }
        /// <summary>
        /// The hero class of the commander that must be selected for this perk to be active.
        /// </summary>
        public string? CommanderSubType { get; set; }
    }
}
