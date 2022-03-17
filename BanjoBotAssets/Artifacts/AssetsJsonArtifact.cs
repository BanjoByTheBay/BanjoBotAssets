using BanjoBotAssets.Artifacts.Helpers;
using BanjoBotAssets.Artifacts.Models;
using BanjoBotAssets.Config;
using Newtonsoft.Json;

namespace BanjoBotAssets.Artifacts
{
    internal sealed class AssetsJsonArtifact : IExportArtifact
    {
        private readonly ExportedFileOptions options;
        private readonly ILogger<AssetsJsonArtifact> logger;
        private readonly IgnoreImagePathsContractResolver contractResolver;

        public AssetsJsonArtifact(ExportedFileOptions options, ILogger<AssetsJsonArtifact> logger, IgnoreImagePathsContractResolver contractResolver)
        {
            this.options = options;
            this.logger = logger;
            this.contractResolver = contractResolver;
        }

        public Task RunAsync(ExportedAssets exportedAssets, IList<ExportedRecipe> exportedRecipes, CancellationToken cancellationToken = default)
        {
            // export assets.json
            var path = string.IsNullOrEmpty(options.Path) ? Resources.File_assets_json : options.Path;

            cancellationToken.ThrowIfCancellationRequested();

            var settings = new JsonSerializerSettings { Formatting = Formatting.Indented, ContractResolver = contractResolver };
            var serializer = JsonSerializer.CreateDefault(settings);

            if (options.Merge && File.Exists(path))
            {
                logger.LogInformation(Resources.Status_MergingIntoExistingArtifact, path);

                ExportedAssets previous;
                using (var stream = File.OpenText(path))
                {
                    var rdr = new JsonTextReader(stream);
                    previous = serializer.Deserialize<ExportedAssets>(rdr)
                        ?? throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.Error_CannotReadPreviousArtifact, path));
                }

                previous.Merge(exportedAssets);
                exportedAssets = previous;
            }
            else
            {
                logger.LogInformation(Resources.Status_WritingFreshArtifact, path);
            }

            using (var file = File.CreateText(path))
            {
                serializer.Serialize(file, exportedAssets);
            }

            return Task.CompletedTask;
        }
    }
}
