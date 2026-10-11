using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text;
using Erlang.Compiler;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Erlang.Differential;

public static class GeneratedModuleCompiler
{
    public static GeneratedModuleArtifact Compile(string source)
    {
        string generated = CodeGeneration.CompileModule(source, GeneratedModuleMetadata.SourceFile);
        string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(generated)));
        var compilation = CSharpCompilation.Create(
            GeneratedModuleMetadata.AssemblyPrefix + hash,
            [CSharpSyntaxTree.ParseText(generated, new CSharpParseOptions(LanguageVersion.Preview))],
            GeneratedModuleReferences.All,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Release)
        );
        using var binary = new MemoryStream();
        var emitted = compilation.Emit(binary);
        if (!emitted.Success)
            throw new InvalidOperationException(OracleDiagnostics.GeneratedCompilation(emitted.Diagnostics.Where(item => item.Severity == DiagnosticSeverity.Error).Select(item => item.ToString())));
        binary.Position = 0;
        var context = new AssemblyLoadContext(GeneratedModuleMetadata.AssemblyPrefix + hash, isCollectible: true);
        try
        {
            var assembly = context.LoadFromStream(binary);
            var definition = assembly.GetTypes().Single(type => type.GetProperty(GeneratedModuleMetadata.DefinitionProperty) is not null).GetProperty(GeneratedModuleMetadata.DefinitionProperty)?.GetValue(null) as ModuleDefinition ?? throw new InvalidOperationException(OracleDiagnostics.GeneratedDefinitionMissing);

            return new GeneratedModuleArtifact(context, definition, hash);
        }
        catch
        {
            context.Unload();
            throw;
        }
    }
}
