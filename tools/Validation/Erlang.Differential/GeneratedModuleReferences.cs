using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Erlang.Differential;

internal static class GeneratedModuleReferences
{
    public static ImmutableArray<MetadataReference> All { get; } = Build();

    private static ImmutableArray<MetadataReference> Build()
    {
        string[] platform = ((string?)AppContext.GetData(GeneratedModuleMetadata.PlatformAssemblies) ?? throw new InvalidOperationException(OracleDiagnostics.PlatformAssembliesMissing)).Split(Path.PathSeparator);

        return platform.Concat(new[] { typeof(Erlang.Compiler.ModuleDefinition).Assembly.Location, typeof(ModuleRegistry).Assembly.Location, typeof(Term).Assembly.Location }).Distinct(StringComparer.Ordinal).Select(path => (MetadataReference)MetadataReference.CreateFromFile(path)).ToImmutableArray();
    }
}
