using System.Diagnostics.CodeAnalysis;

namespace BanjoBotAssets.Artifacts.Models
{
    internal class AlterationSlot
    {
        public int RequiredLevel { get; set; }
        [DisallowNull]
        public string[][]? Alterations { get; set; }
    }
}
