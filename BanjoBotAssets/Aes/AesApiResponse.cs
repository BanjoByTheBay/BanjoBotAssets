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
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

using System.Text.Json.Serialization;

namespace BanjoBotAssets.Aes
{
    public class AesApiResponse
    {
        [JsonPropertyName("mainKey")]
        public string MainKey { get; set; }

        [JsonPropertyName("dynamicKeys")]
        public DynamicKey[] DynamicKeys { get; set; }
    }

    public class DynamicKey
    {
        [JsonPropertyName("name")]
        public string PakFilename { get; set; }

        [JsonPropertyName("guid")]
        public string PakGuid { get; set; }

        [JsonPropertyName("key")]
        public string Key { get; set; }
    }
}
