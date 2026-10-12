using System.Globalization;
using System.Numerics;
using System.Text;

namespace Erlang.Compiler;

public sealed record ModuleDefinition(string Name, IReadOnlyList<(string Name, int Arity)> Exports, IReadOnlyList<FunctionDefinition> Functions)
{
    public void Register(ModuleRegistry registry)
    {
        Otp.OtpModules.Register(registry);
        AtomNameRegistry.Observe(Name);
        foreach (var function in Functions)
            AtomNameRegistry.Observe(function.Name);

        registry.Apply(target =>
        {
            foreach (var export in Exports)
                target.Register(
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
            target.PublishModule(Name);
        });
    }
}
