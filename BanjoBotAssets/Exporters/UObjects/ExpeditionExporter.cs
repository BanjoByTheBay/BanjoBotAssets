using BanjoBotAssets.UExports;
using CUE4Parse.FN.Structs.GA;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BanjoBotAssets.Exporters.UObjects
{
    internal sealed class ExpeditionExporter(IExporterContext services) : UObjectExporter<UObject, ExpeditionItemData>(services)
    {
        protected override string Type => "Expedition";

        protected override bool InterestedInAsset(string name)
        {
            return name.Contains("/Expeditions/Expedition_", StringComparison.OrdinalIgnoreCase);
        }

        protected override Task<bool> ExportAssetAsync(UObject asset, ExpeditionItemData itemData, Dictionary<ImageType, string> imagePaths)
        {
            if (asset.GetOrDefault<FDataTableRowHandle>("ExpeditionRules") is FDataTableRowHandle expeditionRules && 
                expeditionRules.DataTable.TryGetDataTableRow(expeditionRules.RowName.Text, StringComparison.OrdinalIgnoreCase, out var rulesRow))
            {
                FRecipe rulesRecipe = new(rulesRow);
                itemData.ResearchCost = rulesRecipe.RecipeCosts.Length > 0 ? rulesRecipe.RecipeCosts[0].Quantity : 0;
                itemData.ExpeditionType = rulesRecipe.RequiredCatalysts.GameplayTags[0].TagName.Text;
                var reward = rulesRecipe.RecipeResults[0].ItemPrimaryAssetId;
                //is there a better way to format rewards?
                itemData.ExpeditionReward = $"{reward.PrimaryAssetType.Name.Text}:{reward.PrimaryAssetName.Text}";
            }
            return base.ExportAssetAsync(asset, itemData, imagePaths);
        }
    }
}
