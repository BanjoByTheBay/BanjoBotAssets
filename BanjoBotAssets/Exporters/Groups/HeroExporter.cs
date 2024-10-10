/* Copyright 2023 Tara "Dino" Cassatt
 * 
 * This file is part of BanjoBotAssets.
 * 
 * BanjoBotAssets is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * BanjoBotAssets is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with BanjoBotAssets.  If not, see <http://www.gnu.org/licenses/>.
 */
using CUE4Parse.UE4.Objects.GameplayTags;
using CUE4Parse.Utils;

namespace BanjoBotAssets.Exporters.Groups
{
    internal sealed record HeroItemGroupFields(string DisplayName, string? Description, string? SubType,
        string? HeroPerkName, string HeroPerk, string HeroPerkDescription, string? CommanderPerkName, string CommanderPerk, string CommanderPerkDescription,
        PerkRequirement? HeroPerkRequirement, string[] HeroAbilities)
        : BaseItemGroupFields(DisplayName, Description, SubType)
    {
        public HeroItemGroupFields() : this("", null, null, null, "", "", null, "", "", null, []) { }
    }

    internal sealed partial class HeroExporter(IExporterContext services) : GroupExporter<UFortHeroType, BaseParsedItemName, HeroItemGroupFields, HeroItemData>(services)
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

            // hero/commander perk
            var (heroPerkName, heroPerk, heroPerkDesc, heroPerkRequirement) = await GetPerkAsync(hgd, "HeroPerk");
            var (commanderPerkName, commanderPerk, commanderPerkDesc, _) = await GetPerkAsync(hgd, "CommanderPerk");

            // abilities
            var tierAbilityKits = hgd?.GetOrDefault<FStructFallback[]>("TierAbilityKits");
            var heroAbilities = tierAbilityKits != null ? Array.ConvertAll(tierAbilityKits, GetHeroAbilityID) : Array.Empty<string>();

            return result with
            {
                HeroPerkName = heroPerkName,
                HeroPerk = heroPerk,
                HeroPerkDescription = heroPerkDesc,
                HeroPerkRequirement = heroPerkRequirement,
                CommanderPerkName = commanderPerkName,
                CommanderPerk = commanderPerk,
                CommanderPerkDescription = commanderPerkDesc,
                SubType = GetHeroClass(asset.GameplayTags),
                HeroAbilities = heroAbilities,
            };
        }

        private static string GetHeroAbilityID(FStructFallback kit) =>
            kit.GetSoftAssetPath("GrantedAbilityKit") is string s ? $"Ability:{s.SubstringAfterLast('.')}" : "";

        protected override EFortRarity GetRarity(BaseParsedItemName parsedName, UFortHeroType primaryAsset, HeroItemGroupFields fields)
        {
            // SR heroes can be legendary or mythic
            if (parsedName.Rarity.Equals("SR", StringComparison.OrdinalIgnoreCase) && primaryAsset.Rarity == EFortRarity.Mythic)
                return EFortRarity.Mythic;

            return base.GetRarity(parsedName, primaryAsset, fields);
        }

        private async Task<(string? perkName, string displayName, string description, PerkRequirement? requirement)> GetPerkAsync(UObject? gameplayDefinition, string perkProperty)
        {
            var perk = gameplayDefinition?.GetOrDefault<FStructFallback>(perkProperty);
            if (perk == null)
                return (null, $"<{Resources.Field_Hero_NoGrantedAbility}>", $"<{Resources.Field_NoDescription}>", null);

            Interlocked.Increment(ref assetsLoaded);
            var perkName = perk.GetOrDefault<FSoftObjectPath>("GrantedAbilityKit").AssetPathName.Text.Split(".")[^1];
            var grantedAbilityKit = await perk.GetOrDefault<FSoftObjectPath>("GrantedAbilityKit").LoadAsync(provider);
            var displayName = grantedAbilityKit.GetOrDefault<FText>("ItemName")?.Text ?? grantedAbilityKit.GetOrDefault<FText>("DisplayName")?.Text ?? $"<{grantedAbilityKit.Name ?? Resources.Field_Hero_NoGrantedAbility}>";
            var description = await abilityDescription.GetForPerkAbilityKitAsync(grantedAbilityKit, this) ?? $"<{Resources.Field_NoDescription}>";

            PerkRequirement? requirement = null;
            if (perk.GetOrDefault<FStructFallback>("RequiredCommanderTagQuery") is FStructFallback commanderTagQuery)
            {
                requirement = new PerkRequirement
                {
                    Description = perk.GetOrDefault<FText>("CommanderRequirementsText")?.Text ?? "",
                };

                // instead of parsing the TagDictionary and numeric QueryTokenStream, parse the textual AutoDescription
                var expression = commanderTagQuery.GetOrDefault<string>("AutoDescription");
                if (CommanderTagsQueryRegex().Match(expression) is { Success: true } match)
                {
                    var tags = match.Groups["tag"].Captures.Select(c => c.Value).ToArray();

                    // these are either hero ability tags (Granted.Ability.CLASSNAME.ABILITYNAME),
                    // or class perk tags (Granted.Perk.CLASSNAME.PERKNAME[.*]) which we interpret as a class requirement
                    if (tags.Length == 1 && tags[0].StartsWith("Granted.Perk.", StringComparison.OrdinalIgnoreCase))
                    {
                        requirement.CommanderSubType = tags[0].Split('.')[2];
                    }
                    else
                    {
                        requirement.CommanderTag = tags;
                    }
                }
            }

            return (perkName, displayName, description, requirement);
        }

        [GeneratedRegex("^\\s*(?:ANY|ALL)\\(\\s*(?<tag>[a-z0-9.]+)(?:\\s*,\\s*(?<tag>[a-z0-9.]+))*\\s*\\)\\s*$", RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
        private static partial Regex CommanderTagsQueryRegex();

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
                var quest = entry.Get<string>("QuestTemplateID");   // "ID" in uppercase
                heroToTeamPerk.Add(hero, questToTeamPerk[quest]);
            }
        }

        protected override Task<bool> ExportAssetAsync(BaseParsedItemName parsed, UFortHeroType primaryAsset, HeroItemGroupFields fields, string path, HeroItemData itemData)
        {
            itemData.HeroPerkName = fields.HeroPerkName;
            itemData.HeroPerk = fields.HeroPerk;
            itemData.HeroPerkDescription = fields.HeroPerkDescription;
            itemData.CommanderPerkName = fields.CommanderPerkName;
            itemData.CommanderPerk = fields.CommanderPerk;
            itemData.CommanderPerkDescription = fields.CommanderPerkDescription;
            itemData.HeroAbilities = fields.HeroAbilities;
            itemData.HeroPerkRequirement = fields.HeroPerkRequirement;

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
                var text = tag.ToString();
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

        [GeneratedRegex(".*/([^/]+)_(C|UC|R|VR|SR|UR)_T(\\d+)(?:\\..*)?$", RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
        private static partial Regex HeroAssetNameRegex();
    }
}
