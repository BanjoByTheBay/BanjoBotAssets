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

namespace BanjoBotAssets.Json
{
    /// <summary>
    /// Allows a subclass of <see cref="NamedItemData"/> to be deserialized polymorphically
    /// based on the value of the <see cref="NamedItemData.Type"/> field.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class NamedItemDataAttribute(string typeFieldDiscriminator) : Attribute
    {
        /// <summary>
        /// Gets a value that, if found in <see cref="NamedItemData.Type"/> when deserializing,
        /// will signal that the object should be deserialized as an instance of the class marked
        /// with this attribute.
        /// </summary>
        public string TypeFieldDiscriminator { get; } = typeFieldDiscriminator;
    }
}
