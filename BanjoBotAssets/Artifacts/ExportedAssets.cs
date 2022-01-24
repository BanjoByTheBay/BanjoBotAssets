namespace BanjoBotAssets.Models
{
    internal class ExportedAssets
    {
        public DateTime ExportedAt { get; set; } = DateTime.Now;

        public Dictionary<string, NamedItemData> NamedItems { get; } = new(StringComparer.OrdinalIgnoreCase);

        public ItemRatingTables ItemRatings { get; } = new();

        public Dictionary<string, DifficultyInfo> DifficultyInfo { get; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
