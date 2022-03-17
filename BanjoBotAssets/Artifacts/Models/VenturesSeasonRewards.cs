using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;

namespace BanjoBotAssets.Artifacts.Models
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    class VenturesSeason
    {
        [DisallowNull]
        public string? EventTag { get; set; }
        public string? TheaterId { get; set; }
        public string? TheaterName { get; set; }
        public string[]? SeasonalModifiers { get; set; }
        public string[]? ChallengeQuests { get; set; }
        public string? EventCurrency { get; set; }
        public string? DominantElement { get; set; }
        public IList<VenturesSeasonLevel> Levels { get; } = new List<VenturesSeasonLevel>();
        public IList<IList<QuestReward>> PastLevels { get; } = new List<IList<QuestReward>>();
        public int PastLevelXPRequirement { get; set; }
    }

    class VenturesSeasonLevel
    {
        public int TotalRequiredXP { get; set; }
        public bool IsMajorReward { get; set; }
        public IList<QuestReward> Rewards { get; } = new List<QuestReward>();
    }
}