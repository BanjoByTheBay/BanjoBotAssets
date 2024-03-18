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

        private Task VerifyOutput(string source, CancellationToken cancellationToken = default)
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

            var generator = new ExporterRegistrationGenerator();

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

            return VerifyOutput(source);
        }
    }
}