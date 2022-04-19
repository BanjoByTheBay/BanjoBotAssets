using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;

namespace BanjoBotAssets.Artifacts.Models
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    [JsonConverter(typeof(NamedItemDataConverter))]
    internal class NamedItemData
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
        public int? Tier { get; set; }
        [JsonProperty(Order = OrderedPropertiesContractResolver.DefaultOrder + 2)]
        public SortedDictionary<ImageType, string>? ImagePaths { get; set; }
    }
}
