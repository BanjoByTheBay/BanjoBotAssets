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
using CUE4Parse.FN.Structs.GA;
using CUE4Parse.UE4.Objects.Engine.Curves;

namespace BanjoBotAssets.Extensions
{
    internal static class FScalableFloatExtensions
    {
        /// <summary>
        /// Returns the scalable float's <see cref="FScalableFloat.Value">Value</see>, multiplied by
        /// the value read from the <see cref="FScalableFloat.Curve">Curve</see> if it exists.
        /// </summary>
        /// <param name="row">The scalable float.</param>
        /// <returns>The scaled value.</returns>
        public static float GetScaledValue(this FScalableFloat row, ILogger logger)
        {
            var multiplier = row.Value;
            var curveTableRow = row.Curve;
            var curveTable = curveTableRow?.CurveTable;

            if (curveTableRow?.RowName.IsNone != false || curveTable == null)
            {
                return multiplier;
            }

            // find the right FName to use, what a pain
            var rowNameStr = curveTableRow.RowName.Text;
            var curveName = curveTable.RowMap.Keys.FirstOrDefault(k => k.Text == rowNameStr);

            if (curveName.IsNone ||
                curveTableRow.CurveTable?.FindCurve(curveName) is not FRealCurve curve)
            {
                logger.LogWarning(Resources.Warning_MissingCurveTableRow, curveTable.Name, rowNameStr);
                return multiplier;
            }

            return curve.Eval(1) * multiplier;
        }
    }
}
