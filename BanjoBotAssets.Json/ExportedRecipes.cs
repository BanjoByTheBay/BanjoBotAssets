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
using Newtonsoft.Json;

namespace BanjoBotAssets.Json
{
    public static class ExportedRecipes
    {
        public static IList<ExportedRecipe> Merge(IList<ExportedRecipe> previous, IList<ExportedRecipe> current)
        {
            // duplicate current and build a map of merge key => array index
            var mySrc = new List<ExportedRecipe?>(current.Count);
            var keyedSrc = new Dictionary<string, int>(current.Count);

            foreach (var er in current)
            {
                keyedSrc.Add(er.GetMergeKey(), mySrc.Count);
                mySrc.Add(er);
            }

            // copy previous into result, taking values from current instead whenever possible
            var result = new List<ExportedRecipe>(previous.Count + (mySrc.Count / 10));

            foreach (var recipe in previous)
            {
                var mergeKey = recipe.GetMergeKey();

                if (keyedSrc.TryGetValue(mergeKey, out var i))
                {
                    var item = mySrc[i];
                    System.Diagnostics.Debug.Assert(item != null);
                    result.Add(item);
                    keyedSrc.Remove(mergeKey);
                    mySrc[i] = null;
                }
                else
                {
                    result.Add(recipe);
                }
            }

            // copy any remaining items from current
            foreach (var recipe in mySrc)
            {
                if (recipe != null)
                {
                    mySrc.Add(recipe);
                }
            }

            return result;
        }

        public static JsonSerializerSettings CreateJsonSerializerSettings()
        {
            var contractResolver = new NullToEmptyStringContractResolver();
            return new JsonSerializerSettings { ContractResolver = contractResolver, Formatting = Formatting.Indented };
        }

        public static JsonSerializer CreateJsonSerializer() => JsonSerializer.CreateDefault(CreateJsonSerializerSettings());
    }
}
