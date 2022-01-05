namespace BanjoBotAssets.Exporters
{
    internal sealed class QuestExporter : UObjectExporter<UFortQuestItemDefinition, QuestItemData>
    {
        public QuestExporter(DefaultFileProvider provider) : base(provider)
        {
        }

        protected override string Type => "Quest";

        protected override bool IgnoreLoadFailures => true;

        protected override bool InterestedInAsset(string name) => name.Contains("/Content/Quests/");

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
            return Task.FromResult(true);
        }
    }
}
