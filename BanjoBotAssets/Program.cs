using BanjoBotAssets;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.FN.Enums.FortniteGame;
using CUE4Parse.FN.Exports.FortniteGame;
using CUE4Parse.FN.Exports.FortniteGame.NoProperties;
using CUE4Parse.FN.Structs.Engine;
using CUE4Parse.FN.Structs.FortniteGame;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Engine;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.GameplayTags;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using CUE4Parse_Fortnite.Enums;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using System.Text.RegularExpressions;

// open PAK files
const string gameDirectory = @"C:\Program Files\Epic Games\Fortnite\FortniteGame\Content\Paks";

var provider = new DefaultFileProvider(
    gameDirectory,
    SearchOption.TopDirectoryOnly,
    false,
    new VersionContainer(EGame.GAME_UE5_0));
provider.Initialize();

// get encryption keys from fortnite-api.com
AesApiResponse? aes;

using (var client = new HttpClient())
{
    aes = await client.GetFromJsonAsync<AesApiResponse>("https://fortnite-api.com/v2/aes");
}

if (aes == null)
    throw new ApplicationException("Bad aes.json");

Console.WriteLine("Submitting main key");
provider.SubmitKey(new FGuid(), new FAesKey(aes.Data.MainKey));

foreach (var dk in aes.Data.DynamicKeys)
{
    Console.WriteLine("Submitting key for {0}", dk.PakFilename);
    provider.SubmitKey(new FGuid(dk.PakGuid), new FAesKey(dk.Key));
}

Console.WriteLine("Loading mappings");
provider.LoadMappings();
provider.LoadLocalization();

Console.WriteLine("Registering export types");
ObjectTypeRegistry.RegisterEngine(typeof(UFortItemDefinition).Assembly);
ObjectTypeRegistry.RegisterClass("FortDefenderItemDefinition", typeof(UFortHeroType));
ObjectTypeRegistry.RegisterClass("FortTrapItemDefinition", typeof(UFortItemDefinition));
ObjectTypeRegistry.RegisterClass("FortAlterationItemDefinition", typeof(UFortItemDefinition));

var export = new Export();
//using var logFile = new StreamWriter("assets.log");

var namedItems = new ConcurrentDictionary<string, NamedItemData>();

var heroAssets = new ConcurrentBag<string>();
var alterationAssets = new ConcurrentBag<string>();
var schematicAssets = new ConcurrentBag<string>();
var teamPerkAssets = new ConcurrentBag<string>();
var questAssets = new ConcurrentBag<string>();
var missionGenAssets = new ConcurrentBag<string>();
var zoneThemeAssets = new ConcurrentBag<string>();
var gadgetAssets = new ConcurrentBag<string>();
var itemRatingAssets = new ConcurrentBag<string>();
var difficultyAssets = new ConcurrentBag<string>();
var zoneRewardAssets = new ConcurrentBag<string>();
var accountResourceAssets = new ConcurrentBag<string>();
var defenderAssets = new ConcurrentBag<string>();
var survivorAssets = new ConcurrentBag<string>();
var craftingAssets = new ConcurrentBag<string>();
var ingredientAssets = new ConcurrentBag<string>();

var weaponAssets = new ConcurrentBag<string>();

var allAssetLists = new[] {
    // DONE
    ("hero", heroAssets),
    ("team perk", teamPerkAssets),
    ("item rating", itemRatingAssets),
    ("gadget", gadgetAssets),
    ("mission gen", missionGenAssets),
    ("difficulty", difficultyAssets),
    ("zone theme", zoneThemeAssets),
    ("zone reward", zoneRewardAssets),
    ("account resource", accountResourceAssets),
    ("defender", defenderAssets),
    ("survivor", survivorAssets),
    ("ingredient", ingredientAssets),
    ("schematic", schematicAssets),

    // WIP
    ("crafting recipes", craftingAssets),

    // TODO
    ("quest", questAssets),
    ("alteration", alterationAssets),

    // NOT EXPORTED DIRECTLY
    ("weapon", weaponAssets),
};

// find interesting assets
foreach (var (name, file) in provider.Files)
{
    if (name.Contains("/Athena/"))
    {
        continue;
    }

    if (name.Contains("/HID_"))
    {
        heroAssets.Add(name);
    }

    if (name.Contains("/AID_") && (name.Contains("/Alteration_v2/") || name.Contains("/Defenders/")))
    {
        alterationAssets.Add(name);
    }

    if (name.Contains("/SID_"))
    {
        schematicAssets.Add(name);
    }

    if (name.Contains("/TPID_"))
    {
        teamPerkAssets.Add(name);
    }

    if (name.Contains("/Quests/"))
    {
        questAssets.Add(name);
    }

    if (name.Contains("/MissionGens/") && name.Contains("/World/"))
    {
        missionGenAssets.Add(name);
    }

    if (name.Contains("/ZoneThemes/") && name.Contains("/BP_ZT_"))
    {
        zoneThemeAssets.Add(name);
    }

    if (name.Contains("/Gadgets/") && name.Contains("/G_"))
    {
        gadgetAssets.Add(name);
    }

    if (name.EndsWith("ItemRating.uasset"))
    {
        itemRatingAssets.Add(name);
    }

    if (name.EndsWith("GameDifficultyGrowthBounds.uasset"))
    {
        difficultyAssets.Add(name);
    }

    if (name.Contains("/ZCP_"))
    {
        zoneRewardAssets.Add(name);
    }

    if (name.Contains("/PersistentResources/"))
    {
        accountResourceAssets.Add(name);
    }

    if (name.Contains("Defenders/DID_"))
    {
        defenderAssets.Add(name);
    }

    if (name.Contains("Workers/Worker") || name.Contains("Managers/Manager"))
    {
        survivorAssets.Add(name);
    }

    if (name.Contains("/CraftingRecipes_New"))
    {
        craftingAssets.Add(name);
    }

    if (name.Contains("/WID_") || name.Contains("/TID_"))
    {
        weaponAssets.Add(name);
    }

    if (name.Contains("Items/Ingredients/Ingredient_"))
    {
        ingredientAssets.Add(name);
    }
}

// log all asset names found to file
//foreach (var (name, assets) in allAssetLists)
//{
//    foreach (var asset in assets)
//    {
//        logFile.WriteLine("{0}: {1}", name, asset);
//    }
//}

var assetsLoaded = 0;
var stopwatch = new Stopwatch();
stopwatch.Start();

await Task.WhenAll(
    ExportHeroes(),
    ExportTeamPerks(),
    ExportItemRatings(),
    ExportGadgets(),
    ExportMissionGens(),
    ExportDifficulty(),
    ExportZoneThemes(),
    ExportZoneRewards(),
    ExportAccountResources(),
    ExportDefenders(),
    ExportSurvivors(),
    ExportIngredients(),
    ExportSchematics(),
    ExportAlterations()
);

stopwatch.Stop();

Console.WriteLine("Loaded {0} assets in {1} ({2} ms per asset)", assetsLoaded, stopwatch.Elapsed, stopwatch.ElapsedMilliseconds / assetsLoaded);

// export
foreach (var ni in namedItems)
{
    export.NamedItems.Add(ni.Key, ni.Value);
}

using (var file = File.CreateText("assets.json"))
{
    var settings = new JsonSerializerSettings { Formatting = Formatting.Indented };
    var serializer = JsonSerializer.CreateDefault(settings);
    serializer.Serialize(file, export);
}

// done!
return 0;

/********************* ALTERATIONS *********************/

async Task ExportAlterations() => await ExportObjects<UFortItemDefinition>("Alteration", alterationAssets,
    (alteration, exported) =>
    {
        exported.DisplayName = alteration.Description.Text;
        exported.Description = null;
        return Task.FromResult(true);
    });

/********************* INGREDIENTS *********************/

async Task ExportIngredients() => await ExportObjects<UFortIngredientItemDefinition>("Ingredient", ingredientAssets);

/********************* SCHEMATICS *********************/

async Task ExportSchematics()
{
    // load crafting recipes table asset
    var craftingFile = provider[craftingAssets.Single()];
    var craftingTable = await provider.LoadObjectAsync<UDataTable>(craftingFile.PathWithoutExtension);

    if (craftingTable == null)
    {
        Console.WriteLine("WARNING: Can't load crafting recipes");
        return;
    }

    // index weapon asset paths
    var weaponPaths = weaponAssets.ToDictionary(
        path => Path.GetFileNameWithoutExtension(path),
        StringComparer.OrdinalIgnoreCase);

    // load schematic assets
    Regex schematicAssetNameRegex = new(@".*/([^/]+?)(?:_(C|UC|R|VR|SR|UR))?(?:_(Ore|Crystal))?(?:_?T(\d+))?(?:\..*)?$", RegexOptions.IgnoreCase);

    (string baseName, string rarity, string evoType, int? tier)? ParseSchematicAssetName(string path)
    {
        var match = schematicAssetNameRegex.Match(path);

        if (!match.Success)
        {
            Console.WriteLine("WARNING: Can't parse schematic name: {0}", path);
            return null;
        }

        return (baseName: match.Groups[1].Value,
            rarity: match.Groups[2].Value,
            evoType: match.Groups[3].Value,
            tier: match.Groups[4].Success ? int.Parse(match.Groups[4].Value) : null);
    }

    async Task<UFortItemDefinition?> LoadWeaponDefinitionAsync(FDataTableRowHandle craftingRowHandle)
    {
        if (!craftingTable.TryGetDataTableRow(craftingRowHandle.RowName.Text,
            StringComparison.OrdinalIgnoreCase, out var craftingRow))
        {
            Console.WriteLine("WARNING: Can't find crafting row {0}", craftingRowHandle.RowName);
            return null;
        }

        var recipeResults = craftingRow.GetOrDefault<FFortItemQuantityPair[]>("RecipeResults");
        var assetName = recipeResults[0].ItemPrimaryAssetId.PrimaryAssetName.Text;
        if (!weaponPaths.TryGetValue(assetName, out var widPath))
        {
            Console.WriteLine("WARNING: No weapon path indexed for {0}", assetName);
            return null;
        }
        var widFile = provider[widPath];
        try
        {
            return await provider.LoadObjectAsync<UFortItemDefinition>(widFile.PathWithoutExtension);
        }
        catch
        {
            Debugger.Break();
            throw;
        }
    }

    Regex schematicSubTypeRegex = new(@"^(?:Weapon\.(?:Ranged|Melee\.(?:Edged|Blunt|Piercing))|Trap(?=\.(?:Ceiling|Floor|Wall)))\.([^.]+)", RegexOptions.IgnoreCase);

    string SubTypeFromTags(FGameplayTagContainer tags)
    {
        foreach (var tag in tags.GameplayTags)
        {
            var match = schematicSubTypeRegex.Match(tag.Text);

            if (match.Success)
            {
                switch (match.Groups[1].Value.ToLower())
                {
                    case "hammer":
                        return "Hardware";

                    case "heavy":
                        return "Launcher";

                    case "improvised":
                        return "Club";

                    case "smg":
                        return "SMG";

                    default:
                        var sb = new StringBuilder(match.Groups[1].Value);

                        sb[0] = char.ToUpper(sb[0]);

                        for (int i = 1; i < sb.Length; i++)
                            sb[i] = char.ToLower(sb[i]);

                        return sb.ToString();
                }
            }
        }

        return "Unknown";
    }

    string GetSchematicTemplateID(string path) => $"Schematic:{Path.GetFileNameWithoutExtension(path)}";

    var uniqueSchematics = schematicAssets.ToLookup(path => ParseSchematicAssetName(path)?.baseName);
    var numUniqueSchematics = uniqueSchematics.Count;
    var schematicsSoFar = 0;

    await Parallel.ForEachAsync(uniqueSchematics, async (grouping, _cancellationToken) =>
    {
        var baseName = grouping.Key;
        var file = provider[grouping.First()];

        var num = Interlocked.Increment(ref schematicsSoFar);
        Console.WriteLine("Processing schematic group {0} of {1}", num, numUniqueSchematics);

        Console.WriteLine("Loading {0}", file.PathWithoutExtension);
        Interlocked.Increment(ref assetsLoaded);
        var schematic = await provider.LoadObjectAsync(file.PathWithoutExtension);

        if (schematic == null)
        {
            Console.WriteLine("Failed to load {0}", file.PathWithoutExtension);
            return;
        }

        var craftingRow = schematic.GetOrDefault<FDataTableRowHandle>("CraftingRecipe");
        if (craftingRow == null)
        {
            Console.WriteLine("WARNING: No crafting row listed for schematic {0}", schematic.Name);
            return;
        }

        var weaponDef = await LoadWeaponDefinitionAsync(craftingRow);

        if (weaponDef == null)
        {
            Console.WriteLine("WARNING: No weapon definition for crafting row {0}", craftingRow.RowName);
            return;
        }

        var displayName = weaponDef.DisplayName?.Text ?? $"<{baseName}>";
        var description = weaponDef.Description?.Text;
        var subType = SubTypeFromTags(weaponDef.GameplayTags);

        foreach (var path in grouping)
        {
            var templateId = GetSchematicTemplateID(path);
            var parsed = ParseSchematicAssetName(path);

            if (parsed == null)
                continue;

            var rarity = parsed.Value.rarity.ToUpper() switch
            {
                "C" => EFortRarity.Common,
                "R" => EFortRarity.Rare,
                "VR" => EFortRarity.Epic,
                "SR" => EFortRarity.Legendary,
                "UR" => EFortRarity.Mythic,
                _ => EFortRarity.Uncommon,
            };

            namedItems.TryAdd(templateId, new NamedItemData
            {
                AssetPath = provider.FixPath(Path.Combine(Path.GetDirectoryName(path)!, Path.GetFileNameWithoutExtension(path))),
                Description = description,
                DisplayName = displayName.Trim(),
                Name = Path.GetFileNameWithoutExtension(path),
                SubType = subType,
                Type = "Schematic",
                Rarity = rarity.GetNameText().Text,
                Tier = parsed.Value.tier,
            });
        }
    });
}

/********************* SURVIVORS *********************/

static string GetManagerJob(UFortWorkerType worker) =>
    worker.ManagerSynergyTag.First().Text switch
    {
        _ when !worker.bIsManager => throw new ApplicationException("Not a manager"),
        "Homebase.Manager.IsDoctor" => "Doctor",
        "Homebase.Manager.IsEngineer" => "Engineer",
        "Homebase.Manager.IsExplorer" => "Explorer",
        "Homebase.Manager.IsGadgeteer" => "Gadgeteer",
        "Homebase.Manager.IsInventor" => "Inventor",
        "Homebase.Manager.IsMartialArtist" => "Martial Artist",
        "Homebase.Manager.IsSoldier" => "Marksman",
        "Homebase.Manager.IsTrainer" => "Trainer",
        var other => throw new ApplicationException("Unexpected manager synergy " + other),
    };

static string MakeSurvivorDisplayName(UFortWorkerType worker) =>
    worker.bIsManager ? $"Lead {GetManagerJob(worker)}" : $"Survivor";

async Task ExportSurvivors()
{
    // regular survivor:    WorkerBasic_SR_T02
    // special survivor:    Worker_Leprechaun_VR_T01
    // mythic survivor:     Worker_Karoline_UR_T02
    // lead:                ManagerEngineer_R_T04
    // mythic lead:         ManagerMartialArtist_SR_samurai_T03
    Regex survivorAssetNameRegex = new(@".*/([^/]+)_(C|UC|R|VR|SR|UR)_([a-z]+_)?T(\d+)(?:\..*)?$");

    (string baseName, string rarity, int tier)? ParseSurvivorAssetName(string path)
    {
        var match = survivorAssetNameRegex.Match(path);

        if (!match.Success)
        {
            Console.WriteLine("WARNING: Can't parse survivor name: {0}", path);
            return null;
        }

        return (baseName: match.Groups[1].Value + match.Groups[3].Value, rarity: match.Groups[2].Value, tier: int.Parse(match.Groups[4].Value));
    }

    static string GetSurvivorTemplateID(string path) => $"Worker:{Path.GetFileNameWithoutExtension(path)}";

    var uniqueSurvivors = survivorAssets.ToLookup(path => ParseSurvivorAssetName(path)?.baseName);
    var numUniqueSurvivors = uniqueSurvivors.Count;
    var survivorsSoFar = 0;

    await Parallel.ForEachAsync(uniqueSurvivors, async (grouping, _cancellationToken) =>
    {
        var baseName = grouping.Key;
        var file = provider[grouping.First()];

        var num = Interlocked.Increment(ref survivorsSoFar);
        Console.WriteLine("Processing worker group {0} of {1}", num, numUniqueSurvivors);

        Console.WriteLine("Loading {0}", file.PathWithoutExtension);
        Interlocked.Increment(ref assetsLoaded);
        var survivor = await provider.LoadObjectAsync<UFortWorkerType>(file.PathWithoutExtension);

        if (survivor == null)
        {
            Console.WriteLine("Failed to load {0}", file.PathWithoutExtension);
            return;
        }

        var subType = survivor.bIsManager ? GetManagerJob(survivor) : null;
        var description = survivor.Description?.Text;

        foreach (var path in grouping)
        {
            var templateId = GetSurvivorTemplateID(path);
            var parsed = ParseSurvivorAssetName(path);

            if (parsed == null)
                continue;

            var rarity = parsed.Value.rarity switch
            {
                "C" => EFortRarity.Common,
                "R" => EFortRarity.Rare,
                "VR" => EFortRarity.Epic,
                "SR" => EFortRarity.Legendary,
                "UR" => EFortRarity.Mythic,
                _ => EFortRarity.Uncommon,
            };

            var displayName = survivor.DisplayName?.Text ?? MakeSurvivorDisplayName(survivor);

            namedItems.TryAdd(templateId, new NamedItemData
            {
                AssetPath = provider.FixPath(Path.Combine(Path.GetDirectoryName(path)!, Path.GetFileNameWithoutExtension(path))),
                Description = description,
                DisplayName = displayName.Trim(),
                Name = Path.GetFileNameWithoutExtension(path),
                SubType = subType,
                Type = "Worker",
                Rarity = rarity.GetNameText().Text,
                Tier = parsed.Value.tier,
            });
        }
    });
}

/********************* DEFENDERS *********************/

async Task ExportDefenders()
{
    Regex defenderAssetNameRegex = new(@".*/([^/]+)_(C|UC|R|VR|SR|UR)_T(\d+)(?:\..*)?$");

    (string baseName, string rarity, int tier)? ParseDefenderAssetName(string path)
    {
        var match = defenderAssetNameRegex.Match(path);

        if (!match.Success)
        {
            Console.WriteLine("WARNING: Can't parse defender name: {0}", path);
            return null;
        }

        return (baseName: match.Groups[1].Value, rarity: match.Groups[2].Value, tier: int.Parse(match.Groups[3].Value));
    }

    static string GetDefenderTemplateID(string path) => $"Defender:{Path.GetFileNameWithoutExtension(path)}";

    var uniqueDefenders = defenderAssets.ToLookup(path => ParseDefenderAssetName(path)?.baseName);
    var numUniqueDefenders = uniqueDefenders.Count;
    var defendersSoFar = 0;

    await Parallel.ForEachAsync(uniqueDefenders, async (grouping, _cancellationToken) =>
    {
        var baseName = grouping.Key;
        var file = provider[grouping.First()];

        var num = Interlocked.Increment(ref defendersSoFar);
        Console.WriteLine("Processing defender group {0} of {1}", num, numUniqueDefenders);

        Console.WriteLine("Loading {0}", file.PathWithoutExtension);
        Interlocked.Increment(ref assetsLoaded);
        var defender = await provider.LoadObjectAsync<UFortHeroType>(file.PathWithoutExtension);

        if (defender == null)
        {
            Console.WriteLine("Failed to load {0}", file.PathWithoutExtension);
            return;
        }

        var category = defender.AttributeInitKey?.AttributeInitCategory.Text;
        string? subType;

        if (category != null)
        {
            var i = category.LastIndexOf('_');
            var weapon = category[(i + 1)..];

            subType = $"{weapon} Defender";
        }
        else
        {
            subType = null;
        }

        var description = defender.Description?.Text;

        foreach (var path in grouping)
        {
            var templateId = GetDefenderTemplateID(path);
            var parsed = ParseDefenderAssetName(path);

            if (parsed == null)
                continue;

            var rarity = parsed.Value.rarity switch
            {
                "C" => EFortRarity.Common,
                "R" => EFortRarity.Rare,
                "VR" => EFortRarity.Epic,
                "SR" => EFortRarity.Legendary,
                "UR" => EFortRarity.Mythic,
                _ => EFortRarity.Uncommon,
            };

            var displayName = defender.DisplayName?.Text ?? $"{rarity.GetNameText()} {subType ?? "Defender"}";

            namedItems.TryAdd(templateId, new NamedItemData
            {
                AssetPath = provider.FixPath(Path.Combine(Path.GetDirectoryName(path)!, Path.GetFileNameWithoutExtension(path))),
                Description = description,
                DisplayName = displayName.Trim(),
                Name = Path.GetFileNameWithoutExtension(path),
                SubType = subType,
                Type = "Defender",
                Rarity = rarity.GetNameText().Text,
                Tier = parsed.Value.tier,
            });
        }
    });
}

/********************* ACCOUNT RESOURCES *********************/

async Task ExportAccountResources() => await ExportUObjects("AccountResource", accountResourceAssets);

/********************* ZONE REWARDS *********************/

async Task ExportZoneRewards() => await ExportUObjects("CardPack", zoneRewardAssets);

/********************* ZONE THEMES *********************/

async Task ExportZoneThemes() => await ExportBlueprintObjects("ZoneTheme", zoneThemeAssets,
    "ZoneName", "ZoneDescription");

/********************* DIFFICULTY *********************/

async Task ExportDifficulty()
{
    var growthBoundsPath = difficultyAssets.First(p => Path.GetFileNameWithoutExtension(p) == "GameDifficultyGrowthBounds");

    if (growthBoundsPath == null)
    {
        Console.WriteLine("WARNING: GameDifficultyGrowthBounds not found");
        return;
    }

    var file = provider[growthBoundsPath];

    Interlocked.Increment(ref assetsLoaded);
    var dataTable = await provider.LoadObjectAsync<UDataTable>(file.PathWithoutExtension);

    if (dataTable == null)
    {
        Console.WriteLine("WARNING: Could not load {0}", growthBoundsPath);
        return;
    }

    foreach (var (rowKey, data) in dataTable.RowMap)
    {
        var requiredRating = data.GetOrDefault<int>("RequiredRating");
        var maximumRating = data.GetOrDefault<int>("MaximumRating");
        var recommendedRating = data.GetOrDefault<int>("RecommendedRating");
        var displayName = data.GetOrDefault<FText>("ThreatDisplayName")?.Text ?? $"<{recommendedRating}>";

        export.DifficultyInfo.Add(rowKey.Text, new DifficultyInfo
        {
            RequiredRating = requiredRating,
            MaximumRating = maximumRating,
            RecommendedRating = recommendedRating,
            DisplayName = displayName.Trim(),
        });
    }
}

/********************* MISSION GENS *********************/

async Task ExportMissionGens() => await ExportBlueprintObjects("MissionGen", missionGenAssets,
    "MissionName", "MissionDescription");

/********************* GADGETS *********************/

async Task ExportGadgets() => await ExportObjects<UFortGadgetItemDefinition>("Gadget", gadgetAssets,
    async (gadget, exported) =>
    {
        if (gadget.GameplayAbility.AssetPathName.IsNone)
        {
            Console.WriteLine("Skipping gadget with no gameplay ability: {0}", gadget.Name);
            return false;
        }

        var gameplayAbility = await gadget.GameplayAbility.LoadAsync(provider);
        exported.Description = await GetAbilityDescriptionAsync(gameplayAbility);
        return true;
    });

/********************* ITEM RATINGS *********************/

async Task ExportItemRatings()
{
    var defaultTask = ExportDefaultItemRatings();
    var survivorTask = ExportSurvivorItemRatings();

    await Task.WhenAll(defaultTask, survivorTask);
}

async Task ExportDefaultItemRatings()
{
    var baseItemRatingPath = itemRatingAssets.FirstOrDefault(p => Path.GetFileNameWithoutExtension(p) == "BaseItemRating");

    if (baseItemRatingPath == null)
    {
        Console.WriteLine("WARNING: BaseItemRating not found");
        return;
    }

    var file = provider[baseItemRatingPath];

    Interlocked.Increment(ref assetsLoaded);
    var curveTable = await provider.LoadObjectAsync<UCurveTable>(file.PathWithoutExtension);

    if (curveTable == null)
    {
        Console.WriteLine("WARNING: Could not load {0}", baseItemRatingPath);
        return;
    }

    export.ItemRatings.Default = EvaluateItemRatingCurve(curveTable, "Default");
}

async Task ExportSurvivorItemRatings()
{
    var survivorItemRatingPath = itemRatingAssets.FirstOrDefault(p => Path.GetFileNameWithoutExtension(p) == "SurvivorItemRating");

    if (survivorItemRatingPath == null)
    {
        Console.WriteLine("WARNING: SurvivorItemRating not found");
        return;
    }

    var file = provider[survivorItemRatingPath];

    Interlocked.Increment(ref assetsLoaded);
    var curveTable = await provider.LoadObjectAsync<UCurveTable>(file.PathWithoutExtension);

    if (curveTable == null)
    {
        Console.WriteLine("WARNING: Could not load {0}", survivorItemRatingPath);
        return;
    }

    export.ItemRatings.Survivor = EvaluateItemRatingCurve(curveTable, "Default");
    export.ItemRatings.LeadSurvivor = EvaluateItemRatingCurve(curveTable, "Manager", true);
}

ItemRatingTable EvaluateItemRatingCurve(UCurveTable curveTable, string prefix, bool skipUR = false)
{
    (string rarity, int maxTier)[] rarityTiers =
    {
        ("C", 2),
        ("UC", 3),
        ("R", 4),
        ("VR", 5),
        ("SR", 5),
        ("UR", 5),
    };

    (int tier, int minLevel, int maxLevel)[] tierLevels =
    {
        (1, 1, 10),
        (2, 10, 20),
        (3, 20, 30),
        (4, 30, 40),
        (5, 40, 60),    // tier 5 goes up to LV 60 with superchargers
    };

    var tiers = new Dictionary<string, ItemRatingTier>();

    foreach (var (rarity, maxTier) in rarityTiers)
    {
        if (skipUR && rarity == "UR")
            continue;

        foreach (var (tier, minLevel, maxLevel) in tierLevels)
        {
            if (tier > maxTier)
                break;

            var rowNameStr = $"{prefix}_{rarity}_T{tier:00}";
            var rowFName = curveTable.RowMap.Keys.FirstOrDefault(k => k.Text == rowNameStr);

            if (rowFName.IsNone)
            {
                Console.WriteLine("WARNING: Curve table has no row {0}", rowNameStr);
                continue;
            }

            var curve = curveTable.FindCurve(rowFName);

            if (curve == null)
            {
                Console.WriteLine("WARNING: Could not find curve {0}", rowNameStr);
                continue;
            }

            var values = new List<float>();
            
            for (int level = minLevel; level <= maxLevel; level++)
            {
                values.Add(curve.Eval(level));
            }

            tiers.Add($"{rarity}_T{tier:00}",
                new ItemRatingTier { FirstLevel = minLevel, Ratings = values.ToArray() });
        }
    }

    return new ItemRatingTable { Tiers = tiers };
}

/********************* TEAM PERKS *********************/

async Task ExportTeamPerks() => await ExportUObjects("TeamPerk", teamPerkAssets, async (teamPerk, exported) =>
{
    var grantedAbilityKit = await teamPerk.GetOrDefault<FSoftObjectPath>("GrantedAbilityKit").LoadAsync(provider);
    exported.Description = await GetAbilityDescriptionAsync(grantedAbilityKit) ?? "<No description>";
    return true;
});

static async Task<string?> GetAbilityDescriptionAsync(UObject? grantedAbilityKit)
{
    var tooltip = grantedAbilityKit?.GetOrDefault<UBlueprintGeneratedClass>("Tooltip");
    var cdo = tooltip == null ? null : await tooltip.ClassDefaultObject.LoadAsync();
    return cdo?.GetOrDefault<FText>("Description")?.Text;
}

/********************* HEROES *********************/

async Task ExportHeroes()
{
    Regex heroAssetNameRegex = new(@".*/([^/]+)_(C|UC|R|VR|SR|UR)_T(\d+)(?:\..*)?$");

    (string baseName, string rarity, int tier)? ParseHeroAssetName(string path)
    {
        var match = heroAssetNameRegex.Match(path);

        if (!match.Success)
        {
            Console.WriteLine("WARNING: Can't parse hero name: {0}", path);
            return null;
        }

        return (baseName: match.Groups[1].Value, rarity: match.Groups[2].Value, tier: int.Parse(match.Groups[3].Value));
    }

    static string GetHeroTemplateID(string path) => $"Hero:{Path.GetFileNameWithoutExtension(path)}";

    var uniqueHeroes = heroAssets.ToLookup(path => ParseHeroAssetName(path)?.baseName);
    var numUniqueHeroes = uniqueHeroes.Count;
    var heroesSoFar = 0;

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

        async Task<(string displayName, string description)> GetPerkTextAsync(string perkProperty)
        {
            var perk = gameplayDefinition?.GetOrDefault<FStructFallback>(perkProperty);
            var grantedAbilityKit = perk == null ? null : await perk.GetOrDefault<FSoftObjectPath>("GrantedAbilityKit").LoadAsync(provider);
            var displayName = grantedAbilityKit?.GetOrDefault<FText>("DisplayName")?.Text ?? $"<{grantedAbilityKit?.Name ?? "<No granted ability>"}>";
            var description = await GetAbilityDescriptionAsync(grantedAbilityKit) ?? "<No description>";
            return (displayName, description);
        }

        var heroPerk = await GetPerkTextAsync("HeroPerk");
        var commanderPerk = await GetPerkTextAsync("CommanderPerk");

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

            namedItems.TryAdd(templateId, new HeroItemData
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

    static string GetHeroClass(FGameplayTagContainer gameplayTags)
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

/********************* GENERIC EXPORTERS *********************/

async Task ExportUObjects(string type, IReadOnlyCollection<string> assetPaths,
    Func<UObject, NamedItemData, Task<bool>>? exporter = null) =>
    await ExportObjects(type, assetPaths, exporter);

async Task ExportObjects<T>(string type, IReadOnlyCollection<string> assetPaths,
    Func<T, NamedItemData, Task<bool>>? exporter = null)
    where T : UObject
{
    var numToProcess = assetPaths.Count;
    var processedSoFar = 0;

    await Parallel.ForEachAsync(assetPaths, async (path, _cancellationToken) =>
    {
        var file = provider![path];

        var num = Interlocked.Increment(ref processedSoFar);
        Console.WriteLine("Processing {0} {1} of {2}", type, num, numToProcess);

        Console.WriteLine("Loading {0}", file.PathWithoutExtension);
        Interlocked.Increment(ref assetsLoaded);

        var uobject = await provider.LoadObjectAsync<T>(file.PathWithoutExtension);

        if (uobject == null)
        {
            Console.WriteLine("Failed to load {0}", file.PathWithoutExtension);
            return;
        }

        var templateId = $"{type}:{uobject.Name}";
        var displayName = uobject.GetOrDefault<FText>("DisplayName")?.Text ?? $"<{uobject.Name}>";
        var description = uobject.GetOrDefault<FText>("Description")?.Text;

        var namedItemData = new NamedItemData
        {
            AssetPath = provider.FixPath(path),
            Name = uobject.Name,
            Type = type,
            DisplayName = displayName.Trim(),
            Description = description,
        };

        if (uobject.GetOrDefault<EFortItemTier>("Tier") is EFortItemTier tier && tier != default(EFortItemTier))
        {
            namedItemData.Tier = (int)tier;
        }

        if (uobject.GetOrDefault<EFortRarity>("Rarity") is EFortRarity rarity && rarity != default(EFortRarity))
        {
            namedItemData.Rarity = rarity.GetNameText().Text;
        }

        if (exporter != null && await exporter(uobject, namedItemData) == false)
        {
            return;
        }

        namedItems!.TryAdd(templateId, namedItemData);
    });
}

async Task ExportBlueprintObjects(string type, IReadOnlyCollection<string> assetPaths, string displayNameProperty,
    string? descriptionProperty, Func<UBlueprintGeneratedClass, UObject, NamedItemData, Task<bool>>? exporter = null)
{
    var numToProcess = assetPaths.Count;
    var processedSoFar = 0;

    await Parallel.ForEachAsync(assetPaths, async (path, _cancellationToken) =>
    {
        var file = provider![path];

        var num = Interlocked.Increment(ref processedSoFar);
        Console.WriteLine("Processing {0} {1} of {2}", type, num, numToProcess);

        Console.WriteLine("Loading {0}", file.PathWithoutExtension);
        Interlocked.Increment(ref assetsLoaded);
        var pkg = await provider.LoadPackageAsync(file.PathWithoutExtension);

        if (pkg.GetExports().First() is not UBlueprintGeneratedClass bpClass)
        {
            Console.WriteLine("Failed to load {0}", file.PathWithoutExtension);
            return;
        }

        var bpClassPath = bpClass.GetPathName();

        var cdo = await bpClass.ClassDefaultObject.LoadAsync();

        var displayName = (await GetInheritedOrDefaultAsync<FText>(cdo, displayNameProperty))?.Text ?? $"<{bpClass.Name}>";
        var description = descriptionProperty == null ? null : (await GetInheritedOrDefaultAsync<FText>(cdo, descriptionProperty))?.Text;

        var namedItemData = new NamedItemData
        {
            AssetPath = file.PathWithoutExtension,
            Description = description,
            DisplayName = displayName.Trim(),
            Name = bpClass.Name,
            Type = type,
        };

        if (exporter != null && await exporter(bpClass, cdo, namedItemData) == false)
        {
            return;
        }

        namedItems!.TryAdd(bpClassPath, namedItemData);
    });
}

static async Task<T?> GetInheritedOrDefaultAsync<T>(UObject obj, string name)
{
    if (obj.GetOrDefault<T>(name) is T ret && !ret.Equals(default(T)))
        return ret;

    if (obj.Template != null)
    {
        var template = await obj.Template.LoadAsync();

        if (template != null)
            return await GetInheritedOrDefaultAsync<T>(template, name);
    }

    return default(T);
}
