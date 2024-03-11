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
using System.Text;

namespace BanjoBotAssets.Json
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    [JsonConverter(typeof(NamedItemDataConverter))]
    public class NamedItemData
    {
        [JsonProperty(Order = 2), DisallowNull]
        public string? Name { get; set; }
        [JsonProperty(Order = 1), DisallowNull]
        public string? Type { get; set; }
        [JsonProperty(Order = OrderedPropertiesContractResolver.DefaultOrder + 1), DisallowNull]
        public string? AssetPath { get; set; }
        [JsonProperty(Order = 3), DisallowNull]
        public string? DisplayName { get; set; }
        public string? Description { get; set; }
        [JsonProperty(Order = 4)]
        public string? SubType { get; set; }
        public string? Rarity { get; set; }
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool IsPermanent { get; set; }
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool IsInventoryLimitExempt { get; set; }
        public int? Tier { get; set; }
        [JsonProperty(Order = OrderedPropertiesContractResolver.DefaultOrder + 2)]
        public SortedDictionary<ImageType, string>? ImagePaths { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.Append(Type);
            sb.Append(": ");
            sb.Append(Name);

            if (DisplayName != null)
            {
                sb.Append(" (");
                sb.Append(DisplayName);
                sb.Append(')');
            }

            return sb.ToString();
        }
    }
}
