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
using System.Net.Http.Json;

namespace BanjoBotAssets.Aes
{
    internal sealed class FortniteApiAesProvider(ILogger<FortniteApiAesProvider> logger, IOptions<AesOptions> options, IHttpClientFactory httpClientFactory) : IAesProvider
    {
        public async Task<AesApiResponse?> TryGetAesAsync(CancellationToken cancellationToken)
        {
            using var client = httpClientFactory.CreateClient();
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                logger.LogInformation(Resources.Status_FetchingAesFromApi, options.Value.AesApiUri);
                return await client.GetFromJsonAsync<AesApiResponse>(options.Value.AesApiUri, cancellationToken);
            }
            catch (Exception)
            {
                // pokemon
                return null;
            }
        }
    }
}
