using CUE4Parse.FileProvider;
using CUE4Parse.FN.Exports.FortniteGame;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.GameplayTags;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse_Fortnite.Enums;
using System.Text.RegularExpressions;

namespace BanjoBotAssets.Exporters
{
    internal sealed class HeroExporter : IExporter
    {
        private readonly List<string> heroAssets = new();

        private readonly DefaultFileProvider provider;

        private int numUniqueHeroes, heroesSoFar, assetsLoaded;


        public HeroExporter(DefaultFileProvider provider)
        {
            this.provider = provider;
        }

        public void CountAssetLoaded()
        {
            Interlocked.Increment(ref assetsLoaded);
        }

        public int AssetsLoaded => assetsLoaded;

        public void ObserveAsset(string name)
        {
            if (name.Contains("/HID_"))
            {
                heroAssets.Add(name);
            }
        }

        static readonly Regex heroAssetNameRegex = new(@".*/([^/]+)_(C|UC|R|VR|SR|UR)_T(\d+)(?:\..*)?$");

        private static (string baseName, string rarity, int tier)? ParseHeroAssetName(string path)
        {
            var match = heroAssetNameRegex.Match(path);

            if (!match.Success)
            {
                Console.WriteLine("WARNING: Can't parse hero name: {0}", path);
                return null;
            }

            return (baseName: match.Groups[1].Value, rarity: match.Groups[2].Value, tier: int.Parse(match.Groups[3].Value));
        }

        private static string GetHeroTemplateID(string path) => $"Hero:{Path.GetFileNameWithoutExtension(path)}";

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

        private async Task<(string displayName, string description)> GetPerkTextAsync(UObject? gameplayDefinition, string perkProperty)
        {
            var perk = gameplayDefinition?.GetOrDefault<FStructFallback>(perkProperty);
            Interlocked.Increment(ref assetsLoaded);
            var grantedAbilityKit = perk == null ? null : await perk.GetOrDefault<FSoftObjectPath>("GrantedAbilityKit").LoadAsync(provider);
            var displayName = grantedAbilityKit?.GetOrDefault<FText>("DisplayName")?.Text ?? $"<{grantedAbilityKit?.Name ?? "<No granted ability>"}>";
            var description = await AbilityDescription.GetAsync(grantedAbilityKit, this) ?? "<No description>";
            return (displayName, description);
        }

        private void Report(IProgress<ExportProgress> progress, string current)
        {
            progress.Report(new ExportProgress
            {
                TotalSteps = numUniqueHeroes,
                CompletedSteps = heroesSoFar,
                AssetsLoaded = assetsLoaded,
                CurrentItem = current
            });
        }

        public async Task ExportAssets(IProgress<ExportProgress> progress, ExportedAssets output)
        {
            var uniqueHeroes = heroAssets.ToLookup(path => ParseHeroAssetName(path)?.baseName);
            numUniqueHeroes = uniqueHeroes.Count;

            Report(progress, "Exporting heroes");

            await Parallel.ForEachAsync(uniqueHeroes, async (grouping, _cancellationToken) =>
            {
                var baseName = grouping.Key;
                // load the SR version if available so we can know if it's a mythic hero
                var firstSRAssetPath =
                        grouping.FirstOrDefault(p => ParseHeroAssetName(p)?.rarity == "SR") ??
                        grouping.First();
                var file = provider[firstSRAssetPath];

                var num = Interlocked.Increment(ref heroesSoFar);
                Console.WriteLine("Processing hero group {0} of {1}", num, numUniqueHeroes);

                //var exportFileName = file.NameWithoutExtension + ".json";

                Console.WriteLine("Loading {0}", file.PathWithoutExtension);
                Interlocked.Increment(ref assetsLoaded);

                Report(progress, file.PathWithoutExtension);

                var hero = await provider.LoadObjectAsync<UFortHeroType>(file.PathWithoutExtension);

                if (hero == null)
                {
                    Console.WriteLine("Failed to load {0}", file.PathWithoutExtension);
                    return;
                }

                if (hero.AttributeInitKey?.AttributeInitCategory.PlainText == "AthenaHero")
                {
                    Console.WriteLine("Skipping Athena hero: {0}", file.PathWithoutExtension);
                    return;
                }

                var displayName = hero.DisplayName?.Text ?? $"<{baseName}>";
                var description = hero.Description?.Text;

                var heroClass = GetHeroClass(hero.GameplayTags);
                var isMythic = hero.Rarity == EFortRarity.Mythic;

                var gameplayDefinition = hero.HeroGameplayDefinition;

      

                var heroPerk = await GetPerkTextAsync(gameplayDefinition, "HeroPerk");
                var commanderPerk = await GetPerkTextAsync(gameplayDefinition, "CommanderPerk");

                Console.WriteLine("{0} is {1} ({2}), granting {3} / {4}", baseName, hero.DisplayName, heroClass, heroPerk.displayName, commanderPerk.displayName);

                foreach (var path in grouping)
                {
                    var templateId = GetHeroTemplateID(path);
                    var parsed = ParseHeroAssetName(path);

                    if (parsed == null)
                        continue;

                    // SR heroes can be Legendary or Mythic
                    var rarity = parsed.Value.rarity switch
                    {
                        "C" => EFortRarity.Common,
                        "R" => EFortRarity.Rare,
                        "VR" => EFortRarity.Epic,
                        "SR" => isMythic ? EFortRarity.Mythic : EFortRarity.Legendary,
                        _ => EFortRarity.Uncommon,
                    };

                    output.NamedItems.TryAdd(templateId, new HeroItemData
                    {
                        AssetPath = provider.FixPath(Path.Combine(Path.GetDirectoryName(path)!, Path.GetFileNameWithoutExtension(path))),
                        Description = description,
                        DisplayName = displayName.Trim(),
                        Name = Path.GetFileNameWithoutExtension(path),
                        SubType = heroClass,
                        Type = "Hero",
                        Rarity = RarityUtil.GetNameText(rarity).Text,
                        Tier = parsed.Value.tier,
                        HeroPerk = heroPerk.displayName,
                        HeroPerkDescription = heroPerk.description,
                        CommanderPerk = commanderPerk.displayName,
                        CommanderPerkDescription = commanderPerk.description,
                    });
                }
            });
        }
    }
}
