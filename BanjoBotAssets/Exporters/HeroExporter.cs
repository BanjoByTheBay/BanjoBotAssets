using CUE4Parse.FileProvider;
using CUE4Parse.FN.Exports.FortniteGame;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse_Fortnite.Enums;
using System.Text.RegularExpressions;

namespace BanjoBotAssets.Exporters
{
    internal sealed class HeroExporter : GroupExporter<UFortHeroType, BaseParsedItemName, HeroItemGroupFields, HeroItemData>
    {
        public HeroExporter(DefaultFileProvider provider) : base(provider)
        {
        }

        protected override string Type => "Hero";

        protected override bool InterestedInAsset(string name) => name.Contains("/HID_");

        protected override string SelectPrimaryAsset(IGrouping<string?, string> assetGroup)
        {
            return assetGroup.FirstOrDefault(p => ParseAssetName(p)?.Rarity == "SR") ??
                   assetGroup.First();
        }

        protected override bool WantThisAsset(UFortHeroType asset)
        {
            return asset.AttributeInitKey?.AttributeInitCategory.PlainText != "AthenaHero";
        }

        static readonly Regex heroAssetNameRegex = new(@".*/([^/]+)_(C|UC|R|VR|SR|UR)_T(\d+)(?:\..*)?$");

        protected override BaseParsedItemName? ParseAssetName(string name)
        {
            var match = heroAssetNameRegex.Match(name);

            if (!match.Success)
            {
                Console.WriteLine("WARNING: Can't parse hero name: {0}", name);
                return null;
            }

            return new BaseParsedItemName(BaseName: match.Groups[1].Value, Rarity: match.Groups[2].Value, Tier: int.Parse(match.Groups[3].Value));
        }

        protected override async Task<HeroItemGroupFields> ExtractCommonFieldsAsync(UFortHeroType asset, IGrouping<string?, string> grouping)
        {
            var result = await base.ExtractCommonFieldsAsync(asset, grouping);

            var hgd = asset.HeroGameplayDefinition;
            var heroPerk = await GetPerkTextAsync(hgd, "HeroPerk");
            var commanderPerk = await GetPerkTextAsync(hgd, "CommanderPerk");

            return result with
            {
                HeroPerk = heroPerk.displayName,
                HeroPerkDescription = heroPerk.description,
                CommanderPerk = commanderPerk.displayName,
                CommanderPerkDescription = commanderPerk.description,
            };
        }

        protected override EFortRarity GetRarity(BaseParsedItemName parsedName, UFortHeroType primaryAsset, HeroItemGroupFields fields)
        {
            // SR heroes can be legendary or mythic
            if (parsedName.Rarity == "SR" && primaryAsset.Rarity == EFortRarity.Mythic)
                return EFortRarity.Mythic;

            return base.GetRarity(parsedName, primaryAsset, fields);
        }

        private async Task<(string displayName, string description)> GetPerkTextAsync(UObject? gameplayDefinition, string perkProperty)
        {
            var perk = gameplayDefinition?.GetOrDefault<FStructFallback>(perkProperty);
            Interlocked.Increment(ref assetsLoaded);
            var grantedAbilityKit = perk == null ? null : await perk.GetOrDefault<FSoftObjectPath>("GrantedAbilityKit").LoadAsync(provider);
            var displayName = grantedAbilityKit?.GetOrDefault<FText>("DisplayName")?.Text ?? $"<{grantedAbilityKit?.Name ?? "<No granted ability>"}>";
            var description = await AbilityDescription.GetAsync(grantedAbilityKit, this) ?? "<No description>";
            return (displayName, description);
        }
    }
}
