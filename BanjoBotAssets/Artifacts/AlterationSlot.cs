using System.Diagnostics.CodeAnalysis;

namespace BanjoBotAssets.Models
{
    internal class AlterationSlot
    {
        public int RequiredLevel { get; set; }
        [DisallowNull]
        public string[][]? Alterations { get; set; }
    }
}
