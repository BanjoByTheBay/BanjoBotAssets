/* Copyright 2024 Tara "Dino" Cassatt
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
using CUE4Parse.UE4.Versions;
using Microsoft.Extensions.Options;

namespace BanjoBotAssets
{
    internal sealed class LanguageProvider
    {
        public ELanguage Language { get; }
        public CultureInfo CultureInfo { get; }

        public LanguageProvider(IOptions<GameFileOptions> options, AbstractVfsFileProvider provider)
        {
            if (!string.IsNullOrEmpty(options.Value.ELanguage) && Enum.TryParse<ELanguage>(options.Value.ELanguage, out var result))
                Language = result;
            else
                Language = Enum.Parse<ELanguage>(Resources.ELanguage);

            CultureInfo = CultureInfo.CreateSpecificCulture(provider.GetLanguageCode(Language));
        }
    }
}
