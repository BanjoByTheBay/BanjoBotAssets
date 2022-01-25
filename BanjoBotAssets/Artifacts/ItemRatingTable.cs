namespace BanjoBotAssets.Artifacts
{
    internal class ItemRatingTable
    {
        // key: $"{rarity}_T{tier:00}", e.g. "SR_T05"
        public Dictionary<string, ItemRatingTier> Tiers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
