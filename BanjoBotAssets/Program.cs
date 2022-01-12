global using CUE4Parse.FileProvider;
global using CUE4Parse.FN.Enums.FortniteGame;
global using CUE4Parse.FN.Exports.FortniteGame;
global using CUE4Parse.FN.Structs.Engine;
global using CUE4Parse.FN.Structs.FortniteGame;
global using CUE4Parse.UE4.Assets.Exports.Engine;
global using CUE4Parse.UE4.Assets.Exports;
global using CUE4Parse.UE4.Assets.Objects;
global using CUE4Parse.UE4.Objects.Core.i18N;
global using CUE4Parse.UE4.Objects.UObject;
global using CUE4Parse_Fortnite.Enums;
global using System.Globalization;
global using System.Text.RegularExpressions;

using BanjoBotAssets;
using BanjoBotAssets.Exporters;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Http.Json;

// open PAK files
var gameDirectories = Settings.Default.GameDirectories.Cast<string>();

var gameDirectory = gameDirectories.FirstOrDefault(d => Directory.Exists(d));

if (gameDirectory == null)
{
    Console.WriteLine(Resources.Error_GameNotFound);
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
    aes = await client.GetFromJsonAsync<AesApiResponse>(Settings.Default.AesApiUri);
}

if (aes == null)
{
    Console.WriteLine(Resources.Error_AesFetchFailed);
    return 2;
}

Console.WriteLine(Resources.Status_SubmittingMainKey);
provider.SubmitKey(new FGuid(), new FAesKey(aes.Data.MainKey));

foreach (var dk in aes.Data.DynamicKeys)
{
    Console.WriteLine(Resources.Status_SubmittingDynamicKey, dk.PakFilename);
    provider.SubmitKey(new FGuid(dk.PakGuid), new FAesKey(dk.Key));
}

Console.WriteLine(Resources.Status_LoadingMappings);
provider.LoadMappings();
Console.WriteLine(Resources.Status_LoadingLocalization);
provider.LoadLocalization(GetLocalizationLanguage());

Console.WriteLine(Resources.Status_RegisteringExportTypes);
ObjectTypeRegistry.RegisterEngine(typeof(UFortItemDefinition).Assembly);
ObjectTypeRegistry.RegisterClass("FortDefenderItemDefinition", typeof(UFortHeroType));
ObjectTypeRegistry.RegisterClass("FortTrapItemDefinition", typeof(UFortItemDefinition));
ObjectTypeRegistry.RegisterClass("FortAlterationItemDefinition", typeof(UFortItemDefinition));
ObjectTypeRegistry.RegisterClass("FortResourceItemDefinition", typeof(UFortWorldItemDefinition));

var exportedAssets = new ExportedAssets();
var exportedRecipes = new List<ExportedRecipe>();
//using var logFile = new StreamWriter("assets.log");

IExporter[] exporters =
{
    new SchematicExporter(provider),
    new IngredientExporter(provider),
    new WorldItemExporter(provider),
    new CraftingRecipeExporter(provider),
    new AlterationExporter(provider),
    new AccountResourceExporter(provider),
    new DefenderExporter(provider),
    new DifficultyExporter(provider),
    new GadgetExporter(provider),
    new HeroExporter(provider),
    new ItemRatingExporter(provider),
    new MissionGenExporter(provider),
    new SurvivorExporter(provider),
    new TeamPerkExporter(provider),
    new ZoneRewardExporter(provider),
    new ZoneThemeExporter(provider),
    new QuestExporter(provider),
};

// find interesting assets
foreach (var (name, file) in provider.Files)
{
    if (name.Contains("/Athena/"))
    {
        continue;
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

var allPrivateExports = new List<IAssetOutput>();

await Task.WhenAll(exporters.Select(e =>
{
    // give the exporter its own output object to use,
    // then combine the results when the task completes
    var privateExport = new AssetOutput();
    allPrivateExports.Add(privateExport);
    return e.ExportAssetsAsync(progress, privateExport);
}));

stopwatch.Stop();

assetsLoaded = exporters.Sum(e => e.AssetsLoaded);

Console.WriteLine(Resources.Status_LoadedAssets, assetsLoaded, stopwatch.Elapsed, stopwatch.ElapsedMilliseconds / Math.Max(assetsLoaded, 1));

// export assets.json
foreach (var privateExport in allPrivateExports)
    privateExport.CopyTo(exportedAssets, exportedRecipes);

allPrivateExports.Clear();

using (var file = File.CreateText(Resources.File_assets_json))
{
    var settings = new JsonSerializerSettings { Formatting = Formatting.Indented };
    var serializer = JsonSerializer.CreateDefault(settings);
    serializer.Serialize(file, exportedAssets);
}

// export schematics.json
var recipesToExclude = new Stack<int>();

var schematicSubTypeToRecipeType = new Dictionary<string, string>
{
    [Resources.Field_Schematic_Ceiling] = Resources.Field_Recipe_Trap,
    [Resources.Field_Schematic_Floor] = Resources.Field_Recipe_Trap,
    [Resources.Field_Schematic_Wall] = Resources.Field_Recipe_Trap,

    [Resources.Field_Schematic_Axe] = Resources.Field_Recipe_Melee,
    [Resources.Field_Schematic_Club] = Resources.Field_Recipe_Melee,
    [Resources.Field_Schematic_Hardware] = Resources.Field_Recipe_Melee,
    [Resources.Field_Schematic_Scythe] = Resources.Field_Recipe_Melee,
    [Resources.Field_Schematic_Spear] = Resources.Field_Recipe_Melee,
    [Resources.Field_Schematic_Sword] = Resources.Field_Schematic_Sword,

    [Resources.Field_Schematic_Assault] = Resources.Field_Recipe_Ranged,
    [Resources.Field_Schematic_Explosive] = Resources.Field_Recipe_Ranged,
    [Resources.Field_Schematic_Pistol] = Resources.Field_Recipe_Ranged,
    [Resources.Field_Schematic_Shotgun] = Resources.Field_Recipe_Ranged,
    [Resources.Field_Schematic_SMG] = Resources.Field_Recipe_Ranged,
    [Resources.Field_Schematic_Sniper] = Resources.Field_Recipe_Ranged,
};

for (int i = 0; i < exportedRecipes.Count; i++)
{
    var recipe = exportedRecipes[i];

    if (recipe.ItemName == null)
    {
        recipesToExclude.Push(i);
        continue;
    }

    // change schematic ID to display name and fill in other fields
    if (!exportedAssets.NamedItems.TryGetValue(recipe.ItemName, out var schematic))
    {
        Console.WriteLine(Resources.Warning_UnmatchedCraftingRecipe, recipe.ItemName);
        recipesToExclude.Push(i);
        continue;
    }

    recipe.ItemName = schematic.DisplayName ?? "";
    recipe.Type = schematic.SubType != null ? schematicSubTypeToRecipeType.GetValueOrDefault(schematic.SubType, "") : "";
    recipe.Subtype = schematic.SubType ?? "";
    recipe.Rarity = schematic.Rarity ?? "";
    recipe.Tier = schematic.Tier ?? 0;

    if (schematic is SchematicItemData { EvoType: string evoType })
        recipe.Material = evoType.ToLower(CultureInfo.InvariantCulture) switch
        {
            "ore" => Resources.Field_Schematic_Ore,
            "crystal" => Resources.Field_Schematic_Crystal,
            _ => $"<{evoType}>",
        };

    // change ingredient IDs to display names
    if (recipe.Ingredient1 != null)
        recipe.Ingredient1 = exportedAssets.NamedItems.GetValueOrDefault(recipe.Ingredient1)?.DisplayName;
    if (recipe.Ingredient2 != null)
        recipe.Ingredient2 = exportedAssets.NamedItems.GetValueOrDefault(recipe.Ingredient2)?.DisplayName;
    if (recipe.Ingredient3 != null)
        recipe.Ingredient3 = exportedAssets.NamedItems.GetValueOrDefault(recipe.Ingredient3)?.DisplayName;
    if (recipe.Ingredient4 != null)
        recipe.Ingredient4 = exportedAssets.NamedItems.GetValueOrDefault(recipe.Ingredient4)?.DisplayName;
    if (recipe.Ingredient5 != null)
        recipe.Ingredient5 = exportedAssets.NamedItems.GetValueOrDefault(recipe.Ingredient5)?.DisplayName;
}

while (recipesToExclude.Count > 0)
    exportedRecipes.RemoveAt(recipesToExclude.Pop());

using (var file = File.CreateText(Resources.File_schematics_json))
{
    var settings = new JsonSerializerSettings { ContractResolver = NullToEmptyStringResolver.Instance, Formatting = Formatting.Indented };
    var serializer = JsonSerializer.CreateDefault(settings);
    serializer.Serialize(file, exportedRecipes);
}

// done!
return 0;

static ELanguage GetLocalizationLanguage()
{
    if (!string.IsNullOrEmpty(Settings.Default.ELanguage) && Enum.TryParse<ELanguage>(Settings.Default.ELanguage, out var result))
        return result;

    return Enum.Parse<ELanguage>(Resources.ELanguage);
}
