using Newtonsoft.Json;

namespace BanjoBotAssets.Artifacts.Models
{
    internal sealed class ExportedAssets
    {
        [JsonProperty(Order = 1)]
        public DateTime ExportedAt { get; set; } = DateTime.Now;

        [JsonProperty(Order = 2)]
        public SortedDictionary<string, NamedItemData> NamedItems { get; } = new(StringComparer.OrdinalIgnoreCase);

        public ItemRatingTables ItemRatings { get; } = new();

        public SortedDictionary<string, DifficultyInfo> DifficultyInfo { get; } = new(StringComparer.OrdinalIgnoreCase);

        public SortedDictionary<string, string[][]> MainQuestLines { get; } = new();

        public SortedDictionary<string, string[][]> EventQuestLines { get; } = new();

        public SortedDictionary<string, VenturesSeason> VenturesSeasons { get; } = new();

        /// <summary>
        /// Merges the contents of another <see cref="ExportedAssets"/> instance into this one.
        /// </summary>
        /// <param name="other">The <see cref="ExportedAssets"/> instance to merge in.</param>
        public void Merge(ExportedAssets other)
        {
            ExportedAt = other.ExportedAt;

            if (other.NamedItems != null)
            {
                foreach (var (k, v) in other.NamedItems)
                {
                    NamedItems[k] = v;
                }
            }

            if (other.ItemRatings != null)
            {
                if (other.ItemRatings.Default != null)
                    ItemRatings.Default = other.ItemRatings.Default;

                if (other.ItemRatings.Survivor != null)
                    ItemRatings.Survivor = other.ItemRatings.Survivor;

                if (other.ItemRatings.LeadSurvivor != null)
                    ItemRatings.LeadSurvivor = other.ItemRatings.LeadSurvivor;
            }

            if (other.DifficultyInfo != null)
            {
                foreach (var (k, v) in other.DifficultyInfo)
                {
                    DifficultyInfo[k] = v;
                }
            }

            if (other.MainQuestLines != null)
            {
                foreach (var (k, v) in other.MainQuestLines)
                {
                    MainQuestLines[k] = v;
                }
            }

            if (other.EventQuestLines != null)
            {
                foreach (var (k, v) in other.EventQuestLines)
                {
                    EventQuestLines[k] = v;
                }
            }

            if (other.VenturesSeasons != null)
            {
                foreach (var (k, v) in other.VenturesSeasons)
                {
                    VenturesSeasons[k] = v;
                }
            }
        }
    }
}
