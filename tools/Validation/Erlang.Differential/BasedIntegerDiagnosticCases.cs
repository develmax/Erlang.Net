namespace Erlang.Differential;

public static class BasedIntegerDiagnosticCases
{
    public static IReadOnlyList<CompiledDiagnosticCase> All { get; } = Array.AsReadOnly(new[]
    {
        Case("base-one", "1#0", "1"),
        Case("base-too-large", "37#0", "37"),
        Case("binary-digit", "2#2", "integer"),
        Case("alphabetic-digit", "16#G", "integer"),
        Case("missing-digit", "16#", "integer"),
        Case("leading-separator", "16#_1", "integer"),
        Case("trailing-separator", "16#1_", "integer"),
        Case("repeated-separator", "16#1__0", "integer")
    });

    private static CompiledDiagnosticCase Case(string name, string literal, string reason) => new(
        "based-integer-diagnostic-" + name,
        "-module(based_integer_bad). -export([run/0]). run()->" + literal + ".",
        reason,
        Kind: DiagnosticCaseKind.BasedInteger
    );
}
