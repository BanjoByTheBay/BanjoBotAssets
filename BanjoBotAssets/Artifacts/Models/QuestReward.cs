using System.Diagnostics.CodeAnalysis;

namespace BanjoBotAssets.Artifacts.Models
{
    internal class QuestReward
    {
        [DisallowNull]
        public string? Item { get; set; }
        public int Quantity { get; set; }
        public bool Hidden { get; set; }
        public bool Selectable { get; set; }
    }
}
