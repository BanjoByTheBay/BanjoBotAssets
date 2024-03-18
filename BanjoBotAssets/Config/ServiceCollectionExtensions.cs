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
using Microsoft.Extensions.Options;

namespace BanjoBotAssets.Extensions
{
    internal static partial class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers an action used to configure all instances of a particular type of options.
        /// </summary>
        /// <typeparam name="TOptions">The options type to be configured.</typeparam>
        /// <typeparam name="TDep1">A dependency used by the action.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <param name="configureOptions">The action used to configure the options.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection ConfigureAll<TOptions, TDep1>(this IServiceCollection services, Action<TOptions, TDep1> configureOptions)
            where TOptions : class
            where TDep1 : class
        {
            return services
                .AddTransient<IConfigureOptions<TOptions>>(sp =>
                    new ConfigureNamedOptions<TOptions, TDep1>(
                        name: null,
                        sp.GetRequiredService<TDep1>(),
                        configureOptions));
        }

        /// <summary>
        /// Adds a transient service of the type specified in <typeparamref name="TService"/> with a factory that creates
        /// an instance of <typeparamref name="TImplementation"/> using a named options snapshot of type <typeparamref name="TOptions"/>,
        /// keyed by the full name of <typeparamref name="TImplementation"/>.
        /// </summary>
        /// <typeparam name="TService">The type of the service to add.</typeparam>
        /// <typeparam name="TImplementation">The type of the implementation to use.</typeparam>
        /// <typeparam name="TOptions">The type of options used by the service.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static OptionsBuilder<TOptions> AddTransientWithNamedOptions<TService, TImplementation, TOptions>(this IServiceCollection services)
            where TService : class
            where TImplementation : TService
            where TOptions : class
        {
            return services
                .AddTransient<TService>(sp =>
                {
                    using var scope = sp.CreateScope();
                    var opts = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<TOptions>>();
                    return ActivatorUtilities.CreateInstance<TImplementation>(sp, opts.Get(typeof(TImplementation).FullName));
                })
                .AddOptions<TOptions>(typeof(TImplementation).FullName);
        }
    }
}
