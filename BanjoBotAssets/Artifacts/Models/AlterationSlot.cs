using System.Diagnostics.CodeAnalysis;

namespace BanjoBotAssets.Artifacts.Models
{
    internal sealed class AlterationSlot
    {
        public int RequiredLevel { get; set; }
        [DisallowNull]
        public string[][]? Alterations { get; set; }
    }
}
