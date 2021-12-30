using CUE4Parse.UE4.Objects.GameplayTags;

namespace BanjoBotAssets.Exporters
{
    internal record HeroItemGroupFields(string DisplayName, string? Description, string? SubType,
        string HeroPerk, string HeroPerkDescription, string CommanderPerk, string CommanderPerkDescription)
        : BaseItemGroupFields(DisplayName, Description, SubType)
    {
        public HeroItemGroupFields() : this("", null, null, "", "", "", "") { }
    }

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
            var (heroPerk, heroPerkDesc) = await GetPerkTextAsync(hgd, "HeroPerk");
            var (commanderPerk, commanderPerkDesc) = await GetPerkTextAsync(hgd, "CommanderPerk");

            return result with
            {
                HeroPerk = heroPerk,
                HeroPerkDescription = heroPerkDesc,
                CommanderPerk = commanderPerk,
                CommanderPerkDescription = commanderPerkDesc,
                SubType = GetHeroClass(asset.GameplayTags),
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

        protected override void LogAssetName(string baseName, HeroItemGroupFields fields)
        {
            Console.WriteLine("{0} is {1} ({2}), granting {3} / {4}",
                baseName, fields.DisplayName, fields.SubType, fields.HeroPerk, fields.CommanderPerk);
        }

        protected override Task<bool> ExportAssetAsync(BaseParsedItemName parsed, UFortHeroType asset, HeroItemGroupFields fields, string path, HeroItemData itemData)
        {
            itemData.HeroPerk = fields.HeroPerk;
            itemData.HeroPerkDescription = fields.HeroPerkDescription;
            itemData.CommanderPerk = fields.CommanderPerk;
            itemData.CommanderPerkDescription = fields.CommanderPerkDescription;

            return Task.FromResult(true);
        }

        private static string GetHeroClass(FGameplayTagContainer gameplayTags)
        {
            foreach (var tag in gameplayTags)
            {
                var text = tag.Text;
                if (text.Contains("IsCommando"))
                    return "Soldier";
                if (text.Contains("IsNinja"))
                    return "Ninja";
                if (text.Contains("IsOutlander"))
                    return "Outlander";
                if (text.Contains("IsConstructor"))
                    return "Constructor";
            }

            return "Unknown";
        }
    }
}
