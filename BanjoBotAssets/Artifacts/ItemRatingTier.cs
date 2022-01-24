using System.Diagnostics.CodeAnalysis;

namespace BanjoBotAssets.Models
{
    internal class ItemRatingTier
    {
        public int FirstLevel { get; set; }
        [DisallowNull]
        public float[]? Ratings { get; set; }
    }
}
