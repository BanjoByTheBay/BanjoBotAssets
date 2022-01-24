using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace BanjoBotAssets
{
    internal class AssetsJsonFinalizer : IExportFinalizer
    {
        private readonly IOptions<ExportedFileOptions<AssetsJsonFinalizer>> options;

        public AssetsJsonFinalizer(IOptions<ExportedFileOptions<AssetsJsonFinalizer>> options)
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
