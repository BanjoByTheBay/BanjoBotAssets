using System.Diagnostics.CodeAnalysis;

namespace BanjoBotAssets.Models
{
    internal class QuestObjective
    {
        [DisallowNull]
        public string? BackendName { get; set; }
        [DisallowNull]
        public string? Description { get; set; }
        [DisallowNull]
        public string? HudShortDescription { get; set; }
        public int Count { get; set; }
    }
}
