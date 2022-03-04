namespace BanjoBotAssets.Extensions
{
    internal static class EFortRarityExtensions
    {
        public static string ToShortString(this EFortRarity rarity) => rarity switch
        {
            EFortRarity.Common => "C",
            EFortRarity.Uncommon => "UC",
            EFortRarity.Rare => "R",
            EFortRarity.Epic => "VR",
            EFortRarity.Legendary => "SR",
            EFortRarity.Mythic => "UR",
            _ => throw new ArgumentOutOfRangeException(nameof(rarity)),
        };
    }
}
