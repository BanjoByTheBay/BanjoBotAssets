using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BanjoBotAssets.Exporters.UObjects
{
    internal sealed class ConsumableAccountItemExporter : UObjectExporter
    {
        public ConsumableAccountItemExporter(IExporterContext services) : base(services) { } 

        protected override string Type => "ConsumableAccountItem";

        protected override bool InterestedInAsset(string name)
        {
            bool interested = name.Contains("/AccountConsumables/", StringComparison.OrdinalIgnoreCase);

            return interested;
        }
    }
}
