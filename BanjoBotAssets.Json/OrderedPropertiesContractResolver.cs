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
using Newtonsoft.Json.Serialization;

namespace BanjoBotAssets.Json
{
    public class OrderedPropertiesContractResolver : DefaultContractResolver
    {
        /// <summary>
        /// The value used for <see cref="JsonPropertyAttribute.Order"/> if it isn't specified
        /// </summary>
        /// <remarks>
        /// This can be used to write some properties at the end of a serialized object, after the
        /// properties with no <see cref="JsonPropertyAttribute.Order"/> specified.
        /// </remarks>
        public const int DefaultOrder = 1_000_000;

        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            var properties = base.CreateProperties(type, memberSerialization);
            return properties.OrderBy(p => p.Order ?? DefaultOrder).ThenBy(p => p.PropertyName, StringComparer.Ordinal).ToList();
        }
    }
}
