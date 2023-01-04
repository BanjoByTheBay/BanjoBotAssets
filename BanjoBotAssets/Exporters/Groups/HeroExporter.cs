using BanjoBotAssets.Artifacts.Models;
using CUE4Parse.UE4.Objects.GameplayTags;

namespace BanjoBotAssets.Exporters.Groups
{
    internal sealed record HeroItemGroupFields(string DisplayName, string? Description, string? SubType,
        string HeroPerk, string HeroPerkDescription, string CommanderPerk, string CommanderPerkDescription)
        : BaseItemGroupFields(DisplayName, Description, SubType)
    {
        public HeroItemGroupFields() : this("", null, null, "", "", "", "") { }
    }

    internal sealed partial class HeroExporter : GroupExporter<UFortHeroType, BaseParsedItemName, HeroItemGroupFields, HeroItemData>
    {
        private string? itemToQuestPath, questRewardsPath;
        private readonly Dictionary<string, string> heroToTeamPerk = new(StringComparer.OrdinalIgnoreCase);

        protected override string Type => "Hero";

        protected override bool InterestedInAsset(string name)
        {
            if (name.EndsWith("/ItemToQuestTable.uasset", StringComparison.OrdinalIgnoreCase))
            {
                itemToQuestPath = name;
            }

            if (name.EndsWith("/QuestRewards.uasset", StringComparison.OrdinalIgnoreCase))
            {
                questRewardsPath = name;
            }

            return name.Contains("/HID_", StringComparison.OrdinalIgnoreCase);
        }

        protected override string SelectPrimaryAsset(IGrouping<string?, string> assetGroup)
        {
            return assetGroup.FirstOrDefault(p => ParseAssetName(p)?.Rarity.Equals("SR", StringComparison.OrdinalIgnoreCase) == true) ??
                   assetGroup.First();
        }

        protected override bool WantThisAsset(UFortHeroType asset)
        {
            return asset.AttributeInitKey?.AttributeInitCategory.PlainText != "AthenaHero";
        }

        private static readonly Regex heroAssetNameRegex = HeroAssetNameRegex();

        public HeroExporter(IExporterContext services) : base(services) { }

        protected override BaseParsedItemName? ParseAssetName(string name)
        {
            var match = heroAssetNameRegex.Match(name);

            if (!match.Success)
            {
                logger.LogWarning(Resources.Warning_CannotParseHeroName, name);
                return null;
            }

            return new BaseParsedItemName(BaseName: match.Groups[1].Value, Rarity: match.Groups[2].Value, Tier: int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture));
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
            if (parsedName.Rarity.Equals("SR", StringComparison.OrdinalIgnoreCase) && primaryAsset.Rarity == EFortRarity.Mythic)
                return EFortRarity.Mythic;

            return base.GetRarity(parsedName, primaryAsset, fields);
        }

        private async Task<(string displayName, string description)> GetPerkTextAsync(UObject? gameplayDefinition, string perkProperty)
        {
            var perk = gameplayDefinition?.GetOrDefault<FStructFallback>(perkProperty);
            if (perk == null)
                return ($"<{Resources.Field_Hero_NoGrantedAbility}>", $"<{Resources.Field_NoDescription}>");

            Interlocked.Increment(ref assetsLoaded);
            var grantedAbilityKit = await perk.GetOrDefault<FSoftObjectPath>("GrantedAbilityKit").LoadAsync(provider);
            var displayName = grantedAbilityKit.GetOrDefault<FText>("DisplayName")?.Text ?? $"<{grantedAbilityKit.Name ?? Resources.Field_Hero_NoGrantedAbility}>";
            var description = await abilityDescription.GetAsync(grantedAbilityKit, this) ?? $"<{Resources.Field_NoDescription}>";
            return (displayName, description);
        }

        protected override void LogAssetName(string baseName, HeroItemGroupFields fields)
        {
            //logger.LogInformation("{0} is {1} ({2}), granting {3} / {4}",
            //    baseName, fields.DisplayName, fields.SubType, fields.HeroPerk, fields.CommanderPerk);
        }

        public override async Task ExportAssetsAsync(IProgress<ExportProgress> progress, IAssetOutput output, CancellationToken cancellationToken)
        {
            if (itemToQuestPath != null && questRewardsPath != null)
            {
                var itemToQuestTask = provider.LoadObjectAsync<UDataTable>(provider.Files[itemToQuestPath].PathWithoutExtension);
                var questRewardsTask = provider.LoadObjectAsync<UDataTable>(provider.Files[questRewardsPath].PathWithoutExtension);

                var itemToQuestTable = await itemToQuestTask;
                var questRewardsTable = await questRewardsTask;

                InitHeroToTeamPerkMapping(itemToQuestTable, questRewardsTable);
            }

            await base.ExportAssetsAsync(progress, output, cancellationToken);
        }

        private void InitHeroToTeamPerkMapping(UDataTable itemToQuestTable, UDataTable questRewardsTable)
        {
            var questToTeamPerk = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var entry in questRewardsTable.RowMap.Values)
            {
                var reward = entry.Get<FName>("TemplateId").Text;

                if (reward.StartsWith("TeamPerk:", StringComparison.OrdinalIgnoreCase))
                {
                    var quest = entry.Get<string>("QuestTemplateId");
                    // one of the early story quests gives multiple team perks, so replace instead of Add
                    questToTeamPerk[quest] = reward;
                }
            }

            foreach (var entry in itemToQuestTable.RowMap.Values)
            {
                var hero = entry.Get<string>("ItemTemplateID");     // "ID" in uppercase
                var quest = entry.Get<string>("QuestTemplateId");   // "Id" in title case
                heroToTeamPerk.Add(hero, questToTeamPerk[quest]);
            }
        }

        protected override Task<bool> ExportAssetAsync(BaseParsedItemName parsed, UFortHeroType primaryAsset, HeroItemGroupFields fields, string path, HeroItemData itemData)
        {
            itemData.HeroPerk = fields.HeroPerk;
            itemData.HeroPerkDescription = fields.HeroPerkDescription;
            itemData.CommanderPerk = fields.CommanderPerk;
            itemData.CommanderPerkDescription = fields.CommanderPerkDescription;

            if (heroToTeamPerk.TryGetValue($"Hero:{Path.GetFileNameWithoutExtension(path)}", out var teamPerk))
            {
                itemData.UnlocksTeamPerk = teamPerk;
            }

            return Task.FromResult(true);
        }

        private static string GetHeroClass(FGameplayTagContainer gameplayTags)
        {
            foreach (var tag in gameplayTags)
            {
                var text = tag.Text;
                if (text.Contains("IsCommando", StringComparison.OrdinalIgnoreCase))
                    return Resources.Field_Hero_Soldier;
                if (text.Contains("IsNinja", StringComparison.OrdinalIgnoreCase))
                    return Resources.Field_Hero_Ninja;
                if (text.Contains("IsOutlander", StringComparison.OrdinalIgnoreCase))
                    return Resources.Field_Hero_Outlander;
                if (text.Contains("IsConstructor", StringComparison.OrdinalIgnoreCase))
                    return Resources.Field_Hero_Constructor;
            }

            return Resources.Field_Hero_Unknown;
        }

        [GeneratedRegex(".*/([^/]+)_(C|UC|R|VR|SR|UR)_T(\\d+)(?:\\..*)?$", RegexOptions.IgnoreCase, "en-US")]
        private static partial Regex HeroAssetNameRegex();
    }
}
