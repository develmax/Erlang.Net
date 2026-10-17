using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Erlang.Compiler;

internal static class SourceCallLocations
{
    private static readonly ConditionalWeakTable<ModuleDefinition, ConcurrentDictionary<int, Term>> Locations = new();

    internal static Term? For(ModuleDefinition? module, int line)
    {
        if (module?.SourceFilePath is not { } file || line <= 0)
            return null;

        return Locations.GetOrCreateValue(module).GetOrAdd(
            line,
            value => Term.List(
                Term.Tuple(Term.A(SourceCallLocationNames.File), Term.String(file)),
                Term.Tuple(Term.A(SourceCallLocationNames.Line), Term.I(value))
            )
        );
    }
}
