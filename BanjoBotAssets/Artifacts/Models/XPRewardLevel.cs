using System.Diagnostics.CodeAnalysis;

namespace BanjoBotAssets.Artifacts.Models
{
    internal sealed class XPRewardLevel
    {
        public int TotalRequiredXP { get; set; }
        public bool IsMajorReward { get; set; }

        [DisallowNull]
        public QuestReward[] Rewards { get; set; } = null!;
    }
}