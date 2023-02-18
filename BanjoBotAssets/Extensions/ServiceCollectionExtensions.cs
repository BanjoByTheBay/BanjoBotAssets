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
using System.Reflection;

// TODO: use a source generator instead of ServiceCollectionExtensions.AddDerivedServices

namespace BanjoBotAssets.Extensions
{
    internal static partial class ServiceCollectionExtensions
    {
        private static bool HasSoloAttribute(Type t) => t.GetCustomAttribute<SoloForTestingAttribute>(false) != null;

        public static IServiceCollection AddDerivedServices<TService, TImplementationBase>(this IServiceCollection services, ServiceLifetime lifetime)
            where TService : class
            where TImplementationBase : class
        {
            var types = from t in typeof(TImplementationBase).Assembly.GetTypes()
                        where typeof(TImplementationBase).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract
                        select t;

            if (types.Any(HasSoloAttribute))
            {
                types = types.Where(HasSoloAttribute);
            }

            foreach (var t in types)
            {
                services.Add(new ServiceDescriptor(typeof(TService), t, lifetime));
            }

            return services;
        }
    }
}
