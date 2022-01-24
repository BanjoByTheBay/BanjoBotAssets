#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

using System.Text.Json.Serialization;

namespace BanjoBotAssets.Exporters
{
    public class AesApiResponse
    {
        [JsonPropertyName("status")]
        public long Status { get; set; }

        [JsonPropertyName("data")]
        public Data Data { get; set; }
    }

    public class Data
    {
        [JsonPropertyName("build")]
        public string Build { get; set; }

        [JsonPropertyName("mainKey")]
        public string MainKey { get; set; }

        [JsonPropertyName("dynamicKeys")]
        public DynamicKey[] DynamicKeys { get; set; }

        [JsonPropertyName("updated")]
        public DateTimeOffset Updated { get; set; }
    }

    public class DynamicKey
    {
        [JsonPropertyName("pakFilename")]
        public string PakFilename { get; set; }

        [JsonPropertyName("pakGuid")]
        public string PakGuid { get; set; }

        [JsonPropertyName("key")]
        public string Key { get; set; }
    }
}
