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
using BanjoBotAssets.Config;
using Microsoft.Extensions.Options;

namespace BanjoBotAssets.Exporters.Helpers
{
    internal interface IExporterContext
    {
        AbstractVfsFileProvider Provider { get; }
        ILoggerFactory LoggerFactory { get; }
        IOptions<PerformanceOptions> PerformanceOptions { get; }
        IOptions<ScopeOptions> ScopeOptions { get; }
        AbilityDescription AbilityDescription { get; }
        CultureInfo ExportCulture { get; }
    }

    internal sealed class ExporterContext(AbstractVfsFileProvider provider,
        ILoggerFactory loggerFactory, IOptions<PerformanceOptions> performanceOptions,
        AbilityDescription abilityDescription, IOptions<ScopeOptions> scopeOptions) : IExporterContext
    {
        public AbstractVfsFileProvider Provider => provider;

        public ILoggerFactory LoggerFactory => loggerFactory;

        public IOptions<PerformanceOptions> PerformanceOptions => performanceOptions;

        public AbilityDescription AbilityDescription => abilityDescription;

        public IOptions<ScopeOptions> ScopeOptions => scopeOptions;

        public CultureInfo ExportCulture => CultureInfo.InvariantCulture;
    }
}
