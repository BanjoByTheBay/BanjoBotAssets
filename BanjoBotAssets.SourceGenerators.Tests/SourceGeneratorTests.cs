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
using BanjoBotAssets.Exporters;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Assets.Exports;
using BanjoBotAssets.Json;

namespace BanjoBotAssets.SourceGenerators.Tests
{
    [TestClass]
    public class SourceGeneratorTests : VerifyBase
    {
        [ModuleInitializer]
        public static void Init()
        {
            VerifySourceGenerators.Initialize();
        }

        private Task VerifyOutput<T>(string source, CancellationToken cancellationToken = default)
            where T : IIncrementalGenerator, new()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(source, cancellationToken: cancellationToken);

            var references = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
                .Select(a => MetadataReference.CreateFromFile(a.Location))
                .Concat(new[]
                {
                    MetadataReference.CreateFromFile(typeof(UObject).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(BaseExporter).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(NamedItemData).Assembly.Location)
                });

            // reuse the same assembly name since BanjoBotAssets has an InternalsVisibleTo attribute for us
            var compilation = CSharpCompilation.Create(
                assemblyName: "BanjoBotAssets.SourceGenerators.Tests",
                syntaxTrees: [syntaxTree],
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var generator = new T();

            var driver = CSharpGeneratorDriver.Create(generator)
                .RunGenerators(compilation, cancellationToken);

            return Verify(driver);
        }

        [TestMethod]
        public Task GeneratesExporterRegistrationCorrectly()
        {
            var source = @"
using BanjoBotAssets.Exporters.Helpers;
namespace BanjoBotAssets.Exporters.UObjects;
class TestExporter(IExporterContext services) : UObjectExporter(services)
{
    protected override string Type => ""Test"";
    protected override bool InterestedInAsset(string name) => false;
}";

            return VerifyOutput<ExporterRegistrationGenerator>(source);
        }

        [TestMethod]
        public Task GeneratesNamedItemDataTypeMapCorrectly()
        {
            var source = @"
namespace BanjoBotAssets.Json;

[NamedItemData(""Foo"")]
public class FooNamedItemData : NamedItemData
{
    public string FooProperty { get; set; }
}

[NamedItemData(""Bar"")]
public class BarNamedItemData : NamedItemData
{
    public string BarProperty { get; set; }
}";

            return VerifyOutput<NamedItemDataTypeMapGenerator>(source);
        }
    }
}