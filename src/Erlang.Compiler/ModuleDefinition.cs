using System.Globalization;
using System.Numerics;
using System.Text;

namespace Erlang.Compiler;

public sealed record ModuleDefinition(string Name, IReadOnlyList<(string Name, int Arity)> Exports, IReadOnlyList<FunctionDefinition> Functions)
{
    public void Register(ModuleRegistry registry)
    {
        foreach (var export in Exports)
            registry.Register(
                Name,
                export.Name,
                export.Arity,
                (c, a) => Execution.InvokeAsync(
                    this,
                    export.Name,
                    c,
                    a
                )
            );
    }
}
