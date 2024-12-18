using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BanjoBotAssets.Json
{
    [NamedItemData("Expedition")]
    public sealed class ExpeditionItemData : NamedItemData
    {
        [DisallowNull]
        public int? ResearchCost { get; set; }
        [DisallowNull]
        public string? ExpeditionType { get; set; }
        [DisallowNull]
        public string? ExpeditionReward { get; set; }
    }
}
