using System.Globalization;
using System.Numerics;
using System.Text;
using System.Security.Cryptography;

namespace Erlang.Compiler;

public sealed record ModuleDefinition(
    string Name,
    IReadOnlyList<(string Name, int Arity)> Exports,
    IReadOnlyList<FunctionDefinition> Functions,
    IReadOnlyList<ModuleAttribute>? Attributes = null
)
{
    public IReadOnlyList<(string Name, int Arity)> DeclaredExports { get; } = Array.AsReadOnly(Exports.ToArray());
    public IReadOnlyList<FunctionDefinition> DeclaredFunctions { get; } = Array.AsReadOnly(Functions.ToArray());
    public IReadOnlyList<(string Name, int Arity)> Exports { get; init; } = PredefinedModuleFunctions.Exports(Exports);
    public IReadOnlyList<FunctionDefinition> Functions { get; init; } = PredefinedModuleFunctions.Add(Name, Functions);
    public IReadOnlyList<ModuleAttribute> Attributes { get; init; } = Array.AsReadOnly((Attributes ?? []).ToArray());

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
            target.PublishModuleCode(
                Name,
                new ModuleCodeDescriptor(
                    Functions.Select(function => (function.Name, function.Arity)),
                    Exports,
                    new BitString(MD5.HashData(Encoding.UTF8.GetBytes(CodeGeneration.ModuleCode(this, string.Empty, false)))),
                    ModuleCodeFingerprints.Compilation(typeof(ModuleDefinition)),
                    Cons.From(Attributes.Select(attribute => Term.Tuple(Term.A(attribute.Name), ModuleCodeAttributes.Values(attribute.Value))))
                )
            );
        });
    }
}
