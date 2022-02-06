using System.ComponentModel.DataAnnotations;

namespace BanjoBotAssets.Config
{
    public class MappingsOptions
    {
        [Url]
        public string MappingsApiUri { get; set; } = null!;
        public string LocalFilePath { get; set; } = null!;
    }
}
