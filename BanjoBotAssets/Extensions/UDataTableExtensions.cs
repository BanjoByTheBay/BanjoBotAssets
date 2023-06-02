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
using System.Linq.Expressions;

namespace BanjoBotAssets.Extensions
{
    internal static class UDataTableExtensions
    {
        public static Dictionary<string, FStructFallback> ToDictionary(this UDataTable table)
        {
            return table.RowMap.ToDictionary(
                pair => pair.Key.Text,
                pair => pair.Value,
                StringComparer.OrdinalIgnoreCase);
        }

        public static Dictionary<string, T> ToDictionary<T>(this UDataTable table)
        {
            var del = StructFallbackFactory<T>.GetDelegate();

            return table.RowMap.ToDictionary(
                pair => pair.Key.Text,
                del,
                StringComparer.OrdinalIgnoreCase);
        }

        private static readonly Type[] ctorParamTypes = { typeof(FStructFallback) };

        private static Func<KeyValuePair<FName, FStructFallback>, T> GetFallbackCtorDelegate<T>()
        {
            var ctor = typeof(T).GetConstructor(ctorParamTypes) ??
                throw new InvalidOperationException($"{typeof(T).Name} has no FStructFallback constructor");

            var param = Expression.Parameter(typeof(KeyValuePair<FName, FStructFallback>), "fallback");
            var valueOfParam = Expression.Property(param, nameof(KeyValuePair<FName, FStructFallback>.Value));
            var newExpr = Expression.New(ctor, valueOfParam);

            return Expression.Lambda<Func<KeyValuePair<FName, FStructFallback>, T>>(newExpr, param).Compile();
        }

        /// <summary>
        /// Caches the delegate for converting <see cref="KeyValuePair{TKey, TValue}"/> to <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">A type with a constructor accepting a single <see cref="FStructFallback"/> parameter.</typeparam>
        private static class StructFallbackFactory<T>
        {
            private static Func<KeyValuePair<FName, FStructFallback>, T>? _delegate;

            public static Func<KeyValuePair<FName, FStructFallback>, T> GetDelegate() =>
                _delegate ??= GetFallbackCtorDelegate<T>();
        }
    }
}
