namespace Erlang.Differential;

public sealed record NativeRecordDiagnosticContract(
    CompiledDiagnosticCase Case,
    string Code,
    string Message,
    Term Reason
);
