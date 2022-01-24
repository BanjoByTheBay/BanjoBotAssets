using BanjoBotAssets.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace BanjoBotAssets.Exporters.Impl
{
    internal class AssetsJsonArtifact : IExportArtifact
    {
        private readonly IOptions<ExportedFileOptions<AssetsJsonArtifact>> options;

        public AssetsJsonArtifact(IOptions<ExportedFileOptions<AssetsJsonArtifact>> options)
        {
            this.options = options;
        }

        public Task RunAsync(ExportedAssets exportedAssets, IList<ExportedRecipe> exportedRecipes, CancellationToken cancellationToken = default)
        {
            // export assets.json
            var path = string.IsNullOrEmpty(options.Value.Path) ? Resources.File_assets_json : options.Value.Path;

            cancellationToken.ThrowIfCancellationRequested();

            using (var file = File.CreateText(path))
            {
                var settings = new JsonSerializerSettings { Formatting = Formatting.Indented };
                var serializer = JsonSerializer.CreateDefault(settings);
                serializer.Serialize(file, exportedAssets);
            }

            return Task.CompletedTask;
        }
    }
}
