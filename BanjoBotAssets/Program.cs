using BanjoBotAssets;
using BanjoBotAssets.Exporters;
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
string[] gameDirectories =
{
    @"C:\Program Files\Epic Games\Fortnite\FortniteGame\Content\Paks",
    @"D:\Program Files\Epic Games\Fortnite\FortniteGame\Content\Paks",
    @"D:\Epic Games\Fortnite\FortniteGame\Content\Paks",
};

var gameDirectory = gameDirectories.FirstOrDefault(d => Directory.Exists(d));

if (gameDirectory == null)
{
    Console.WriteLine("Couldn't find game directory. Add it to the list.");
    return 1;
}

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

var export = new ExportedAssets();
//using var logFile = new StreamWriter("assets.log");

var namedItems = new ConcurrentDictionary<string, NamedItemData>();

//var heroAssets = new ConcurrentBag<string>();
//var alterationAssets = new ConcurrentBag<string>();
var schematicAssets = new ConcurrentBag<string>();
//var teamPerkAssets = new ConcurrentBag<string>();
var questAssets = new ConcurrentBag<string>();
//var missionGenAssets = new ConcurrentBag<string>();
//var zoneThemeAssets = new ConcurrentBag<string>();
var gadgetAssets = new ConcurrentBag<string>();
//var itemRatingAssets = new ConcurrentBag<string>();
//var difficultyAssets = new ConcurrentBag<string>();
var zoneRewardAssets = new ConcurrentBag<string>();
//var accountResourceAssets = new ConcurrentBag<string>();
var defenderAssets = new ConcurrentBag<string>();
var survivorAssets = new ConcurrentBag<string>();
var craftingAssets = new ConcurrentBag<string>();
//var ingredientAssets = new ConcurrentBag<string>();

var weaponAssets = new ConcurrentBag<string>();

var allAssetLists = new[] {
    // DONE
    //("hero", heroAssets),
    //("team perk", teamPerkAssets),
    //("item rating", itemRatingAssets),
    //("gadget", gadgetAssets),
    //("mission gen", missionGenAssets),
    //("difficulty", difficultyAssets),
    //("zone theme", zoneThemeAssets),
    ("zone reward", zoneRewardAssets),
    //("account resource", accountResourceAssets),
    ("defender", defenderAssets),
    ("survivor", survivorAssets),
    //("ingredient", ingredientAssets),
    ("schematic", schematicAssets),
    //("alteration", alterationAssets),

    // WIP
    ("crafting recipes", craftingAssets),

    // TODO
    ("quest", questAssets),

    // NOT EXPORTED DIRECTLY
    ("weapon", weaponAssets),
};

IExporter[] exporters =
{
    new HeroExporter(provider),
    new ZoneThemeExporter(provider),
    new MissionGenExporter(provider),
    new AccountResourceExporter(provider),
    new AlterationExporter(provider),
    new IngredientExporter(provider),
    new TeamPerkExporter(provider),
    new ZoneRewardExporter(provider),
    new GadgetExporter(provider),
    new ItemRatingExporter(provider),
    new DifficultyExporter(provider),
};

// find interesting assets
foreach (var (name, file) in provider.Files)
{
    if (name.Contains("/Athena/"))
    {
        continue;
    }

    if (name.Contains("/SID_"))
    {
        schematicAssets.Add(name);
    }

    if (name.Contains("/Quests/"))
    {
        questAssets.Add(name);
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

    foreach (var e in exporters)
    {
        e.ObserveAsset(name);
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

var progress = new Progress<ExportProgress>(prog =>
{
    // TODO: do something with progress reports
});

var assetsLoaded = 0;
var stopwatch = new Stopwatch();
stopwatch.Start();

await Task.WhenAll(new[] {
    ExportDefenders(),
    ExportSurvivors(),
    ExportSchematics(),
}.Concat(exporters.Select(e => e.ExportAssets(progress, export))));

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
        return await provider.LoadObjectAsync<UFortItemDefinition>(widFile.PathWithoutExtension);
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
