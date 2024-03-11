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
using System.Text;

namespace BanjoBotAssets.Properties
{
    /// <summary>
    /// Contains CompositeFormat instances used throughout the application.
    /// </summary>
    internal static class FormatStrings
    {
        public static readonly CompositeFormat DefenderDisplayName = CompositeFormat.Parse(Resources.FormatString_Field_Defender_DisplayNameFormat);
        public static readonly CompositeFormat DefenderName = CompositeFormat.Parse(Resources.FormatString_Field_Defender_NameFormat);
        public static readonly CompositeFormat ExportingGroup = CompositeFormat.Parse(Resources.FormatString_Status_ExportingGroup);
        public static readonly CompositeFormat SurvivorLeadName = CompositeFormat.Parse(Resources.FormatString_Field_Survivor_LeadNameFormat);
    }
}
