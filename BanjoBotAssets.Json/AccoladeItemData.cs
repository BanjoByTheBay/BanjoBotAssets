using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BanjoBotAssets.Json
{
    [NamedItemData("Accolades")]
    public sealed class AccoladeItemData : NamedItemData
    {
        [DisallowNull]
        public int? AccoladeXP { get; set; }
    }
}
