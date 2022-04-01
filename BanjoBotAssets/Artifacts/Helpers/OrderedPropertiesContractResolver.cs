using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace BanjoBotAssets.Artifacts.Helpers
{
    internal class OrderedPropertiesContractResolver : DefaultContractResolver
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
