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

namespace BanjoBotAssets.Json
{
    public sealed class ExportedAssets
    {
        [JsonProperty(Order = 1)]
        public DateTime ExportedAt { get; set; } = DateTime.Now;

        [JsonProperty(Order = 2)]
        public SortedDictionary<string, NamedItemData> NamedItems { get; } = new(StringComparer.OrdinalIgnoreCase);

        public ItemRatingTables ItemRatings { get; } = new();

        public SortedDictionary<string, DifficultyInfo> DifficultyInfo { get; } = new(StringComparer.OrdinalIgnoreCase);

        public SortedDictionary<string, string[][]> MainQuestLines { get; } = new();

        public SortedDictionary<string, string[][]> EventQuestLines { get; } = new();

        public SortedDictionary<string, VenturesSeason> VenturesSeasons { get; } = new();

        /// <summary>
        /// Merges the contents of another <see cref="ExportedAssets"/> instance into this one.
        /// </summary>
        /// <param name="other">The <see cref="ExportedAssets"/> instance to merge in.</param>
        public void Merge(ExportedAssets other)
        {
            ExportedAt = other.ExportedAt;

            if (other.NamedItems != null)
            {
                foreach (var (k, v) in other.NamedItems)
                {
                    NamedItems[k] = v;
                }
            }

            if (other.ItemRatings != null)
            {
                if (other.ItemRatings.Default != null)
                    ItemRatings.Default = other.ItemRatings.Default;

                if (other.ItemRatings.Survivor != null)
                    ItemRatings.Survivor = other.ItemRatings.Survivor;

                if (other.ItemRatings.LeadSurvivor != null)
                    ItemRatings.LeadSurvivor = other.ItemRatings.LeadSurvivor;
            }

            if (other.DifficultyInfo != null)
            {
                foreach (var (k, v) in other.DifficultyInfo)
                {
                    DifficultyInfo[k] = v;
                }
            }

            if (other.MainQuestLines != null)
            {
                foreach (var (k, v) in other.MainQuestLines)
                {
                    MainQuestLines[k] = v;
                }
            }

            if (other.EventQuestLines != null)
            {
                foreach (var (k, v) in other.EventQuestLines)
                {
                    EventQuestLines[k] = v;
                }
            }

            if (other.VenturesSeasons != null)
            {
                foreach (var (k, v) in other.VenturesSeasons)
                {
                    VenturesSeasons[k] = v;
                }
            }
        }

        public static JsonSerializerSettings CreateJsonSerializerSettings(bool wantImagePaths)
        {
            var contractResolver = new ImagePathsContractResolver(wantImagePaths);
            return new JsonSerializerSettings { ContractResolver = contractResolver, Formatting = Formatting.Indented };
        }

        public static JsonSerializer CreateJsonSerializer(bool wantImagePaths) => JsonSerializer.CreateDefault(CreateJsonSerializerSettings(wantImagePaths));
    }
}
