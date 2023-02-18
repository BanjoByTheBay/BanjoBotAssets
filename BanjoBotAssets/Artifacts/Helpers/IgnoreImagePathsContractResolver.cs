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
using BanjoBotAssets.Artifacts.Models;
using BanjoBotAssets.Config;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Reflection;

namespace BanjoBotAssets.Artifacts.Helpers
{
    internal sealed class IgnoreImagePathsContractResolver : OrderedPropertiesContractResolver
    {
        private readonly IOptions<ImageExportOptions> options;

        public IgnoreImagePathsContractResolver(IOptions<ImageExportOptions> options)
        {
            this.options = options;
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var result = base.CreateProperty(member, memberSerialization);

            // if we aren't exporting any image paths, hide the whole ImagePaths property
            if (typeof(NamedItemData).IsAssignableFrom(member.DeclaringType) && member.Name == nameof(NamedItemData.ImagePaths) &&
                options.Value.Type.Values.All(i => i == WantImageExport.No))
            {
                result.ShouldSerialize = _ => false;
            }

            return result;
        }
    }
}
