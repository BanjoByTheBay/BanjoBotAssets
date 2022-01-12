namespace BanjoBotAssets.Exporters
{
    internal sealed class QuestExporter : UObjectExporter<UFortQuestItemDefinition, QuestItemData>
    {
        private string? questRewardsPath;
        private UDataTable? questRewardsTable;

        public QuestExporter(DefaultFileProvider provider) : base(provider)
        {
        }

        protected override string Type => "Quest";

        protected override bool IgnoreLoadFailures => true;

        protected override bool InterestedInAsset(string name)
        {
            if (name.EndsWith("/QuestRewards.uasset", StringComparison.OrdinalIgnoreCase))
            {
                questRewardsPath = name;
            }

            return name.Contains("/Content/Quests/");
        }

        public override async Task ExportAssetsAsync(IProgress<ExportProgress> progress, IAssetOutput output)
        {
            questRewardsTable = await TryLoadTableAsync(questRewardsPath);

            await base.ExportAssetsAsync(progress, output);
        }

        private async Task<UDataTable?> TryLoadTableAsync(string? path)
        {
            if (path == null)
                return null;

            var file = provider[path];
            Interlocked.Increment(ref assetsLoaded);
            return await provider.LoadObjectAsync<UDataTable>(file.PathWithoutExtension);
        }

        protected override Task<bool> ExportAssetAsync(UFortQuestItemDefinition asset, QuestItemData namedItemData)
        {
            var objectives = new List<QuestObjective>();

            if (asset.Objectives != null)
            {
                foreach (var o in asset.Objectives)
                {
                    var qo = new QuestObjective
                    {
                        BackendName = o.BackendName.Text,
                        Count = o.Count,
                        Description = o.Description.Text,
                        HudShortDescription = o.HudShortDescription.Text,
                    };
                    objectives.Add(qo);
                }
            }

            namedItemData.Objectives = objectives.ToArray();
            namedItemData.Category = asset.Category?.RowName.Text ?? "";

            var rewards = new List<QuestReward>();
            if (questRewardsTable != null)
            {
                // for Quest:daily_destroyarcademachines, we use the rows "Daily_DestroyArcadeMachines_001", "Daily_DestroyArcadeMachines_002", etc.
                var regex = new Regex(@$"^{Regex.Escape(asset.Name)}_\d+$", RegexOptions.IgnoreCase);

                foreach (var (key, reward) in questRewardsTable.RowMap)
                {
                    if (!key.IsNone && regex.IsMatch(key.Text))
                    {
                        rewards.Add(new QuestReward
                        {
                            Item = reward.Get<FName>("TemplateId").Text,
                            Quantity = reward.Get<int>("Quantity"),
                            Hidden = reward.Get<bool>("Hidden"),
                            Selectable = reward.Get<bool>("Selectable"),
                        });
                    }
                }
            }
            namedItemData.Rewards = rewards.ToArray();

            return Task.FromResult(true);
        }
    }
}
