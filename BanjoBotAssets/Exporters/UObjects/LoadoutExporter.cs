using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BanjoBotAssets.Exporters.UObjects
{
    internal sealed class LoadoutExporter : UObjectExporter
    {
        public LoadoutExporter(IExporterContext services) : base(services) { } 

        protected override string Type => "CampaignHeroLoadout";

        protected override bool InterestedInAsset(string name)
        {
            bool interested = name.Contains("/Loadouts/", StringComparison.OrdinalIgnoreCase) && !name.Contains("/Slots/", StringComparison.OrdinalIgnoreCase);

            return interested;
        }
    }
}
