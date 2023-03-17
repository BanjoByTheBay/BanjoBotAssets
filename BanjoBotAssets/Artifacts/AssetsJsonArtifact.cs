/* Copyright 2023 Tara "Dino" Cassatt
 * 
 * This file is part of BanjoBotAssets.
 * 
 * BanjoBotAssets is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * BanjoBotAssets is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with BanjoBotAssets.  If not, see <http://www.gnu.org/licenses/>.
 */
using BanjoBotAssets.Config;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace BanjoBotAssets.Artifacts
{
    internal sealed class AssetsJsonArtifact : IExportArtifact
    {
        private readonly ExportedFileOptions options;
        private readonly ILogger<AssetsJsonArtifact> logger;
        private readonly IOptions<ImageExportOptions> imageExportOptions;

        public AssetsJsonArtifact(ExportedFileOptions options, ILogger<AssetsJsonArtifact> logger, IOptions<ImageExportOptions> imageExportOptions)
        {
            this.options = options;
            this.logger = logger;
            this.imageExportOptions = imageExportOptions;
        }

        public Task RunAsync(ExportedAssets exportedAssets, IList<ExportedRecipe> exportedRecipes, CancellationToken cancellationToken = default)
        {
            // export assets.json
            var path = string.IsNullOrEmpty(options.Path) ? Resources.File_assets_json : options.Path;

            cancellationToken.ThrowIfCancellationRequested();

            var wantImagePaths = imageExportOptions.Value.Type.Values.Any(i => i != WantImageExport.No);
            var serializer = ExportedAssets.CreateJsonSerializer(wantImagePaths);

            if (options.Merge && File.Exists(path))
            {
                logger.LogInformation(Resources.Status_MergingIntoExistingArtifact, path);

                ExportedAssets previous;
                using (var stream = File.OpenText(path))
                {
                    var rdr = new JsonTextReader(stream);
                    previous = serializer.Deserialize<ExportedAssets>(rdr)
                        ?? throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.FormatString_Error_CannotReadPreviousArtifact, path));
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
