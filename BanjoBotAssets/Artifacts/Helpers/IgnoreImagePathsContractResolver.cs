using BanjoBotAssets.Artifacts.Models;
using BanjoBotAssets.Config;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Reflection;

namespace BanjoBotAssets.Artifacts.Helpers
{
    internal class IgnoreImagePathsContractResolver : OrderedPropertiesContractResolver
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
