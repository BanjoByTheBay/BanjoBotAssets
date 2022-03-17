using System.Diagnostics.CodeAnalysis;

namespace BanjoBotAssets.Artifacts.Models
{
    internal class ItemRatingTables
    {
        [DisallowNull]
        public ItemRatingTable? Survivor { get; set; }
        [DisallowNull]
        public ItemRatingTable? LeadSurvivor { get; set; }
        [DisallowNull]
        public ItemRatingTable? Default { get; set; }

        public void Update(ItemRatingTables other)
        {
            Survivor ??= other.Survivor;
            LeadSurvivor ??= other.LeadSurvivor;
            Default ??= other.Default;
        }
    }
}
