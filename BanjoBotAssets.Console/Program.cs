/* Copyright 2026 Tara "Dino" Cassatt
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


using BanjoBotAssets;
using BanjoBotAssets.Exporters;
using BanjoBotAssets.Reporters;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;


await Host.CreateDefaultBuilder(args)
#if DEBUG
    .UseEnvironment("Development")
#endif
    .UseContentRoot(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new InvalidOperationException("Null content root"))
    .ConfigureLogging(logging =>
    {
        logging
            .ClearProviders()
            .AddSimpleConsole(console => console.SingleLine = true);
    })
    .ConfigureServices(services =>
    {
        services
            .AddBanjoServices();

        services.AddSingleton<IExportStageReporter, ExampleStageReporter>();
        services.AddSingleton<IExportProgressReporter, ExampleProgressReporter>();
    })
    .RunConsoleAsync(o => o.SuppressStatusMessages = true);

return Environment.ExitCode;

class ExampleStageReporter : IExportStageReporter
{
    public void Report(ExportStage stage)
    {
        Console.WriteLine($"Reporting Stage: {stage}");
    }
}

class ExampleProgressReporter : IExportProgressReporter
{
    public void Report(ExportProgress progress)
    {
        if (progress.ExportType is not null)
            Console.WriteLine($"Reporting Progress for {progress.ExportType}: {progress.CompletedSteps}/{progress.TotalSteps}");
    }
}