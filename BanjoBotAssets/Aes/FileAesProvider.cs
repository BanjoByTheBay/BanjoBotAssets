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
using System.Text.Json;

namespace BanjoBotAssets.Aes
{
    internal sealed class FileAesProvider(ILogger<FileAesProvider> logger, IOptions<AesOptions> options, GameDirectoryProvider gameDirectoryProvider) : IAesProvider, IAesCacheUpdater
    {
        public async Task<AesApiResponse?> TryGetAesAsync(CancellationToken cancellationToken)
        {
            string localFilePath = options.Value.LocalFilePath;

            if (!File.Exists(localFilePath))
            {
                logger.LogInformation(Resources.Status_MissingAesCache, localFilePath);
                return null;
            }

            if (File.GetLastWriteTime(localFilePath) < gameDirectoryProvider.GetGameDirectory().LastWriteTime)
            {
                logger.LogInformation(Resources.Status_SkippingOutdatedCachedAes, localFilePath);
                return null;
            }

            logger.LogInformation(Resources.Status_LoadingAesFromFile, localFilePath);
            return JsonSerializer.Deserialize<AesApiResponse>(await File.ReadAllTextAsync(localFilePath, cancellationToken));
        }

        public Task UpdateAesCacheAsync(AesApiResponse response, CancellationToken cancellationToken)
        {
            logger.LogInformation(Resources.Status_SavingAesToFile, options.Value.LocalFilePath);
            return File.WriteAllTextAsync(options.Value.LocalFilePath, JsonSerializer.Serialize(response), cancellationToken);
        }
    }
}
