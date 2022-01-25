using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;

namespace BanjoBotAssets.Artifacts
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    internal class NamedItemData
    {
        [DisallowNull]
        public string? Name { get; set; }
        [DisallowNull]
        public string? Type { get; set; }
        [DisallowNull]
        public string? AssetPath { get; set; }
        [DisallowNull]
        public string? DisplayName { get; set; }
        public string? Description { get; set; }
        public string? SubType { get; set; }
        public string? Rarity { get; set; }
        public int? Tier { get; set; }
    }
}
