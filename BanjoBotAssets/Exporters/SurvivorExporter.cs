// TODO: export fixed personalities for mythics

namespace BanjoBotAssets.Exporters
{
    internal sealed class SurvivorExporter : GroupExporter<UFortWorkerType>
    {
        public SurvivorExporter(DefaultFileProvider provider) : base(provider)
        {
        }

        protected override string Type => "Worker";

        protected override bool InterestedInAsset(string name) =>
            name.Contains("Workers/Worker") || name.Contains("Managers/Manager");


        // regular survivor:    WorkerBasic_SR_T02
        // special survivor:    Worker_Leprechaun_VR_T01
        // mythic survivor:     Worker_Karolina_UR_T02
        // lead:                ManagerEngineer_R_T04
        // mythic lead:         ManagerMartialArtist_SR_samurai_T03
        private static readonly Regex survivorAssetNameRegex = new(@".*/([^/]+)_(C|UC|R|VR|SR|UR)_([a-z]+_)?T(\d+)(?:\..*)?$");

        protected override BaseParsedItemName? ParseAssetName(string name)
        {
            var match = survivorAssetNameRegex.Match(name);

            if (!match.Success)
            {
                Console.WriteLine(Resources.Warning_CannotParseSurvivorName, name);
                return null;
            }

            return new BaseParsedItemName(BaseName: match.Groups[1].Value + match.Groups[3].Value, Rarity: match.Groups[2].Value, Tier: int.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture));
        }

        protected override async Task<BaseItemGroupFields> ExtractCommonFieldsAsync(UFortWorkerType asset, IGrouping<string?, string> grouping)
        {
            var result = await base.ExtractCommonFieldsAsync(asset, grouping);
            var subType = asset.bIsManager ? GetManagerJob(asset) : null;
            var displayName = asset.DisplayName?.Text ?? MakeSurvivorDisplayName(asset);
            return result with { SubType = subType, DisplayName = displayName };
        }

        private static string GetManagerJob(UFortWorkerType worker) =>
            worker.ManagerSynergyTag.First().Text switch
            {
                _ when !worker.bIsManager => throw new AssetFormatException(Resources.Error_NotAManager),
                "Homebase.Manager.IsDoctor" => Resources.Field_Survivor_Doctor,
                "Homebase.Manager.IsEngineer" => Resources.Field_Survivor_Engineer,
                "Homebase.Manager.IsExplorer" => Resources.Field_Survivor_Explorer,
                "Homebase.Manager.IsGadgeteer" => Resources.Field_Survivor_Gadgeteer,
                "Homebase.Manager.IsInventor" => Resources.Field_Survivor_Inventor,
                "Homebase.Manager.IsMartialArtist" => Resources.Field_Survivor_MartialArtist,
                "Homebase.Manager.IsSoldier" => Resources.Field_Survivor_Marksman,
                "Homebase.Manager.IsTrainer" => Resources.Field_Survivor_Trainer,
                var other => throw new AssetFormatException(string.Format(CultureInfo.CurrentCulture, Resources.Error_UnexpectedManagerSynergy, other)),
            };

        private static string MakeSurvivorDisplayName(UFortWorkerType worker) =>
            worker.bIsManager ? string.Format(CultureInfo.CurrentCulture, Resources.Field_Survivor_LeadNameFormat, GetManagerJob(worker)) : Resources.Field_Survivor_DefaultName;
    }
}
