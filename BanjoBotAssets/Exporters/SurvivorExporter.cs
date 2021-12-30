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
                Console.WriteLine("WARNING: Can't parse survivor name: {0}", name);
                return null;
            }

            return new BaseParsedItemName(BaseName: match.Groups[1].Value + match.Groups[3].Value, Rarity: match.Groups[2].Value, Tier: int.Parse(match.Groups[4].Value));
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

        private static string MakeSurvivorDisplayName(UFortWorkerType worker) =>
            worker.bIsManager ? $"Lead {GetManagerJob(worker)}" : "Survivor";
    }
}
