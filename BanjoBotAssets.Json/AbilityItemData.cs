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
namespace BanjoBotAssets.Json
{
    [NamedItemData("Ability")]
    public sealed class AbilityItemData : NamedItemData
    {
        public float? CooldownSeconds { get; set; }
        public string? GrantedTag { get; set; }
        public int? PreferredQuickbarSlot { get; set; }

        public float? EnergyCost { get; set; }
        public AbilityStats? AbilityStats { get; set; }
    }

    /// <summary>
    /// Ability damage stats from the GadgetScaling table
    /// </summary>
    public sealed class AbilityStats
    {
        public float? Damage { get; set; }
        public float? EnvDamage { get; set; }
        public float? ImpactDamage { get; set; }
        public float? BaseCritChance { get; set; }
        public float? BaseCritDamage { get; set; }
        public float? StunTime { get; set; }
    }
}
