using System.ComponentModel.DataAnnotations;

namespace BanjoBotAssets
{
    public class GameFileOptions
    {
        public string[] GameDirectories { get; set; } = null!;
        public string ELanguage { get; set; } = null!;
        [Url]
        public string AesApiUri { get; set; } = null!;

        public string AssetsOutputPath { get; set; } = null!;
        public string SchematicsOutputPath { get; set; } = null!;
    }
}
