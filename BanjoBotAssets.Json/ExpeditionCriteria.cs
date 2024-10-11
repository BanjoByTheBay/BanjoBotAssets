using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BanjoBotAssets.Json
{
    public sealed class ExpeditionCriteria
    {
        [DisallowNull]
        public string? RequiredTag { get; set; }
        public string? RequiredRarity { get; set; }
        [DisallowNull]
        public float ModValue { get; set; }
    }
}
