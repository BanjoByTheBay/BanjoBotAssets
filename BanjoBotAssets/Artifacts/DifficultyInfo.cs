using System.Diagnostics.CodeAnalysis;

namespace BanjoBotAssets.Models
{
    internal class DifficultyInfo
    {
        public int RequiredRating { get; set; }
        public int MaximumRating { get; set; }
        public int RecommendedRating { get; set; }
        [DisallowNull]
        public string? DisplayName { get; set; }
    }
}
