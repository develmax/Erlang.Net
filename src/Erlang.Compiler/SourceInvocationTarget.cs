namespace Erlang.Compiler;

internal sealed record SourceInvocationTarget(
    ModuleDefinition? Module,
    string? Function,
    LocalFunctionIdentity? Identity,
    Expr.Fun? Expression,
    Dictionary<string, Term>? Environment,
    ModuleCodeImage? Image,
    SourceCodeLease? Lease = null
);
