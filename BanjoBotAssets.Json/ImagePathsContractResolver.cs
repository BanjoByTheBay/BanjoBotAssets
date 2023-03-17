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
using System.Reflection;

namespace BanjoBotAssets.Json
{
    internal sealed class ImagePathsContractResolver : OrderedPropertiesContractResolver
    {
        private readonly bool wantImagePaths;

        public ImagePathsContractResolver(bool wantImagePaths)
        {
            this.wantImagePaths = wantImagePaths;
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var result = base.CreateProperty(member, memberSerialization);

            // if we aren't exporting any image paths, hide the whole ImagePaths property
            if (!wantImagePaths && member.Name == nameof(NamedItemData.ImagePaths) &&
                typeof(NamedItemData).IsAssignableFrom(member.DeclaringType))
            {
                result.ShouldSerialize = _ => false;
            }

            return result;
        }
    }
}
