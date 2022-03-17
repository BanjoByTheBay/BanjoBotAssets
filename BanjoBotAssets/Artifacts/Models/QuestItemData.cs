using BanjoBotAssets.Artifacts.Helpers;
using System.Diagnostics.CodeAnalysis;

namespace BanjoBotAssets.Artifacts.Models
{
    [NamedItemData("Quest")]
    internal class QuestItemData : NamedItemData
    {
        [DisallowNull]
        public QuestObjective[]? Objectives { get; set; }
        [DisallowNull]
        public string? Category { get; set; }
        [DisallowNull]
        public QuestReward[]? Rewards { get; set; }
        [DisallowNull]
        public string? QuestType { get; set; }
    }
}
