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

namespace BanjoBotAssets.Exporters.UObjects
{
    internal sealed partial class HeroExporter(IExporterContext services) : UObjectExporter<UFortHeroType, HeroItemData>(services)
    {
        private string? itemToQuestPath, questRewardsPath;
        private readonly Dictionary<string, string> heroToTeamPerk = new(StringComparer.OrdinalIgnoreCase);
        protected override string Type => "Hero";
        protected override bool RequireRarity => true;

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

            //non-STW heroes were previously filtered out by the failure of ParseAssetName in GroupExporter, but since that no longer exists we add a check for "/SaveTheWorld/"
            if (!name.Contains("/SaveTheWorld/", StringComparison.OrdinalIgnoreCase))
                return false;

            return name.Contains("/HID_", StringComparison.OrdinalIgnoreCase);
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
                var reward = entry.Get<FName>("TemplateId", StringComparison.OrdinalIgnoreCase).Text;

                if (reward.StartsWith("TeamPerk:", StringComparison.OrdinalIgnoreCase))
                {
                    var quest = entry.Get<string>("QuestTemplateId");
                    // one of the early story quests gives multiple team perks, so replace instead of Add
                    questToTeamPerk[quest] = reward;
                }
            }

            foreach (var entry in itemToQuestTable.RowMap.Values)
            {
                var hero = entry.Get<string>("ItemTemplateID", StringComparison.OrdinalIgnoreCase);
                var quest = entry.Get<string>("QuestTemplateID", StringComparison.OrdinalIgnoreCase);
                heroToTeamPerk.Add(hero, questToTeamPerk[quest]);
            }
        }

        protected override async Task<bool> ExportAssetAsync(UFortHeroType asset, HeroItemData itemData, Dictionary<ImageType, string> imagePaths)
        {
            itemData.SubType = GetHeroClass(asset.GameplayTags);

            var hgd = asset.HeroGameplayDefinition;

            // hero/commander perk
            var (heroPerkTemplate, heroPerkName, heroPerkDesc, heroPerkRequirement) = await GetPerkAsync(hgd, "HeroPerk");
            itemData.HeroPerkTemplate = heroPerkTemplate;
            itemData.HeroPerk = heroPerkName;
            itemData.HeroPerkDescription = heroPerkDesc;
            itemData.HeroPerkRequirement = heroPerkRequirement;

            var (commanderPerkTemplate, commanderPerkName, commanderPerkDesc, _) = await GetPerkAsync(hgd, "CommanderPerk");
            itemData.CommanderPerkTemplate = commanderPerkTemplate;
            itemData.CommanderPerk = commanderPerkName;
            itemData.CommanderPerkDescription = commanderPerkDesc;

            // abilities
            var tierAbilityKits = hgd?.GetOrDefault<FStructFallback[]>("TierAbilityKits");
            itemData.HeroAbilities = tierAbilityKits != null ? Array.ConvertAll(tierAbilityKits, GetHeroAbilityID) : [];

            if (heroToTeamPerk.TryGetValue($"Hero:{asset.Name}", out var teamPerk))
            {
                itemData.UnlocksTeamPerk = teamPerk;
            }

            return true;
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

        private async Task<(string? templateId, string displayName, string description, PerkRequirement? requirement)> GetPerkAsync(UObject? gameplayDefinition, string perkProperty)
        {
            var perk = gameplayDefinition?.GetOrDefault<FStructFallback>(perkProperty);
            if (perk == null)
                return (null, $"<{Resources.Field_Hero_NoGrantedAbility}>", $"<{Resources.Field_NoDescription}>", null);

            Interlocked.Increment(ref assetsLoaded);
            var templateId = "Ability:"+perk.GetOrDefault<FSoftObjectPath>("GrantedAbilityKit").AssetPathName.Text.Split(".")[^1];
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

            return (templateId, displayName, description, requirement);
        }

        private static string GetHeroAbilityID(FStructFallback kit) =>
            kit.GetSoftAssetPath("GrantedAbilityKit") is string s ? $"Ability:{s.SubstringAfterLast('.')}" : "";

        [GeneratedRegex("^\\s*(?:ANY|ALL)\\(\\s*(?<tag>[a-z0-9.]+)(?:\\s*,\\s*(?<tag>[a-z0-9.]+))*\\s*\\)\\s*$", RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
        private static partial Regex CommanderTagsQueryRegex();
    }
}
