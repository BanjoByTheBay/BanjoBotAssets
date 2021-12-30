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
using System.Collections.Concurrent;
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

var export = new ExportedAssets();
//using var logFile = new StreamWriter("assets.log");

var schematicAssets = new ConcurrentBag<string>();
var questAssets = new ConcurrentBag<string>();
var craftingAssets = new ConcurrentBag<string>();

var weaponAssets = new ConcurrentBag<string>();

var allAssetLists = new[] {
    // WIP
    ("crafting recipes", craftingAssets),

    // TODO
    ("quest", questAssets),

    // NOT EXPORTED DIRECTLY
    ("weapon", weaponAssets),
};

IExporter[] exporters =
{
    new AccountResourceExporter(provider),
    new AlterationExporter(provider),
    new DefenderExporter(provider),
    new DifficultyExporter(provider),
    new GadgetExporter(provider),
    new HeroExporter(provider),
    new IngredientExporter(provider),
    new ItemRatingExporter(provider),
    new MissionGenExporter(provider),
    new SchematicExporter(provider),
    new SurvivorExporter(provider),
    new TeamPerkExporter(provider),
    new ZoneRewardExporter(provider),
    new ZoneThemeExporter(provider),
};

// find interesting assets
foreach (var (name, file) in provider.Files)
{
    if (name.Contains("/Athena/"))
    {
        continue;
    }

    if (name.Contains("/Quests/"))
    {
        questAssets.Add(name);
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

// export
foreach (var privateExport in allPrivateExports)
    privateExport.CopyTo(export);

allPrivateExports.Clear();

using (var file = File.CreateText("assets.json"))
{
    var settings = new JsonSerializerSettings { Formatting = Formatting.Indented };
    var serializer = JsonSerializer.CreateDefault(settings);
    serializer.Serialize(file, export);
}

// done!
return 0;
