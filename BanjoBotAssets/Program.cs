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

Console.WriteLine("Loaded {0} assets in {1} ({2} ms per asset)", assetsLoaded, stopwatch.Elapsed, stopwatch.ElapsedMilliseconds / Math.Max(assetsLoaded, 1));

// export assets.json
foreach (var privateExport in allPrivateExports)
    privateExport.CopyTo(exportedAssets, exportedRecipes);

allPrivateExports.Clear();

using (var file = File.CreateText("assets.json"))
{
    var settings = new JsonSerializerSettings { Formatting = Formatting.Indented };
    var serializer = JsonSerializer.CreateDefault(settings);
    serializer.Serialize(file, exportedAssets);
}

// export schematics.json
var recipesToExclude = new Stack<int>();

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
        Console.WriteLine("WARNING: Crafting recipe with no matching schematic: {0}", recipe.ItemName);
        recipesToExclude.Push(i);
        continue;
    }

    recipe.ItemName = schematic.DisplayName ?? "";
    recipe.Type = schematic.Type ?? "";
    recipe.Subtype = schematic.SubType ?? "";
    recipe.Rarity = schematic.Rarity ?? "";
    recipe.Tier = schematic.Tier ?? 0;

    if (schematic is SchematicItemData { EvoType: string evoType })
        recipe.Material = evoType.CapitalizeFirst();

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

using (var file = File.CreateText("schematics.json"))
{
    var settings = new JsonSerializerSettings { ContractResolver = NullToEmptyStringResolver.Instance, Formatting = Formatting.Indented };
    var serializer = JsonSerializer.CreateDefault(settings);
    serializer.Serialize(file, exportedRecipes);
}

// done!
return 0;
