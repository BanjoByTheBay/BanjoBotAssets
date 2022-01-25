using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;

namespace BanjoBotAssets.Artifacts
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    internal class QuestObjective
    {
        [DisallowNull]
        public string? BackendName { get; set; }
        [DisallowNull]
        public string? Description { get; set; }
        [DisallowNull]
        public string? HudShortDescription { get; set; }
        public string? ObjectiveStatRowName { get; set; }
        public int? ZonePowerLevel { get; set; }
        public int Count { get; set; }
    }
}
