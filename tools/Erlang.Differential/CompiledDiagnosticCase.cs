namespace Erlang.Differential;

public sealed record CompiledDiagnosticCase(
    string Name,
    string Source,
    string Variable,
    string? UnsafeConstruct = null,
    DiagnosticCaseKind Kind = DiagnosticCaseKind.VariableScope
);
