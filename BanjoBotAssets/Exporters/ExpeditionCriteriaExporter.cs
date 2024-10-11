using CUE4Parse.UE4.Objects.GameplayTags;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BanjoBotAssets.Exporters
{
    internal sealed class ExpeditionCriteriaExporter(IExporterContext services) : BaseExporter(services)
    {
        public override async Task ExportAssetsAsync(IProgress<ExportProgress> progress, IAssetOutput output, CancellationToken cancellationToken)
        {
            var criteriaPath = assetPaths.First(p => Path.GetFileNameWithoutExtension(p).Equals("ExpeditionCriteriaRequirements", StringComparison.OrdinalIgnoreCase));

            if (criteriaPath == null)
            {
                logger.LogError(Resources.Error_SpecificAssetNotFound, "ExpeditionCriteriaRequirements");
                return;
            }

            var file = provider[criteriaPath];

            Interlocked.Increment(ref assetsLoaded);
            var dataTable = await provider.LoadObjectAsync<UDataTable>(file.PathWithoutExtension);

            if (dataTable == null)
            {
                logger.LogError(Resources.Warning_CouldNotLoadAsset, criteriaPath);
                return;
            }

            foreach (var (rowKey, data) in dataTable.RowMap)
            {
                var requiredTag = data.GetOrDefault<FGameplayTag>("RequiredTag").TagName.Text;
                var hasRequiredRarity = data.GetOrDefault<bool>("bRequireRarity");
                var requiredRarity = data.GetOrDefault<EFortRarity>("RequiredRarity");
                var modValue = data.GetOrDefault<float>("ModValue");

                output.AddExpeditionCriteria(rowKey.Text, new ExpeditionCriteria
                {
                    RequiredTag = requiredTag,
                    RequiredRarity = hasRequiredRarity ? requiredRarity.GetNameText().Text : null,
                    ModValue = modValue
                });
            }
        }

        protected override bool InterestedInAsset(string name) => name.EndsWith("ExpeditionCriteriaRequirements.uasset", StringComparison.OrdinalIgnoreCase);
    }
}
