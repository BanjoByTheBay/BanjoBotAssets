using System.ComponentModel.DataAnnotations;

namespace BanjoBotAssets.Config
{
    internal class AesOptions
    {
        [Url]
        public string AesApiUri { get; set; } = null!;
        public string LocalFilePath { get; set; } = null!;
    }
}
