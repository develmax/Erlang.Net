namespace Erlang.Differential;

public static class NifDeclarationDiagnosticCases
{
    public static IReadOnlyList<CompiledDiagnosticCase> All { get; } =
    [
        new(
            "nif-declaration-undefined-local",
            "-module(nif_declaration_rejection). -export([run/0]). -nifs([missing/7]). run()->ok.",
            "missing",
            "7",
            DiagnosticCaseKind.NifUndefined
        ),
        new(
            "nif-declaration-invalid-integer-entry",
            "-module(nif_declaration_rejection). -export([run/0]). -nifs([17]). run()->ok.",
            "17",
            Kind: DiagnosticCaseKind.NifInvalidEntry
        )
    ];
}
