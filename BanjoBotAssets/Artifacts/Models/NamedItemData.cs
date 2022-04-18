using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

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

    internal class NamedItemDataConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => typeof(NamedItemData).IsAssignableFrom(objectType);

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);

            string? type = (string?)jo["Type"];
            NamedItemData? result;

            if (string.IsNullOrEmpty(type) || (result = CreateNamedItemDataFromTypeField(type)) == null)
                result = new NamedItemData();

            serializer.Populate(jo.CreateReader(), result);
            return result;
        }

        private static readonly Dictionary<string, Type> NamedItemDataTypeMap = new();

        static NamedItemDataConverter()
        {
            foreach (var type in typeof(NamedItemData).Assembly.GetTypes())
            {
                if (type == typeof(NamedItemData) || !typeof(NamedItemData).IsAssignableFrom(type))
                    continue;

                var attrs = type.GetCustomAttributes<NamedItemDataAttribute>().ToList();

                if (attrs.Count == 0)
                    throw new InvalidOperationException($"Type '{type.FullName}' derives from {nameof(NamedItemData)} but has no {nameof(NamedItemDataAttribute)}");

                foreach (var a in attrs)
                    NamedItemDataTypeMap.Add(a.TypeFieldDiscriminator, type);
            }
        }

        private static NamedItemData? CreateNamedItemDataFromTypeField(string discriminator)
        {
            if (NamedItemDataTypeMap.TryGetValue(discriminator, out var type))
                return (NamedItemData?)Activator.CreateInstance(type);

            return null;
        }

        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
