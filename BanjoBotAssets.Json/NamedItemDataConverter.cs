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
using Newtonsoft.Json.Linq;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("BanjoBotAssets.SourceGenerators.Tests")]

namespace BanjoBotAssets.Json
{
    public sealed partial class NamedItemDataConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => typeof(NamedItemData).IsAssignableFrom(objectType);

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);

            string? type = (string?)jo["Type"];
            NamedItemData? result;

            if (string.IsNullOrEmpty(type) || (result = CreateNamedItemDataFromTypeField(type)) == null)
                result = new NamedItemData();

            serializer.Populate(jo.CreateReader(), result);
            return result;
        }

        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}