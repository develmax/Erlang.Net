namespace Erlang.Differential;

public static class FormBoundaryCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } =
    [
        Create("space", " "),
        Create("tab", "\t"),
        Create("newline", "\n"),
        Create("carriage-return", "\r"),
        Create("nul", "\0"),
        Create("extended-low", "\u0080"),
        Create("extended-high", "\u00a0"),
        Create("comment", "% boundary\n"),
        new(
            "compiler-form-boundary-record-selection",
            "-module(form_boundary_contract). -export([run/0]). -record(r,{value=42}). run()->R=#r{},42=R#r.value,done.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        )
    ];

    public static IReadOnlyList<CompiledDiagnosticCase> Diagnostics { get; } =
    [
        new(
            "form-boundary-exact-aborted-source",
            "-module(function_resolution_contract). -export([run/0]). run()->private=loaded(),done.loaded()->private.",
            "",
            null,
            DiagnosticCaseKind.FormBoundarySyntax
        ),
        new(
            "form-boundary-adjacent-attribute",
            "-module(form_boundary_contract).-export([run/0]). run()->done.",
            "",
            null,
            DiagnosticCaseKind.FormBoundarySyntax
        )
    ];

    private static CompiledModuleCase Create(string name, string separator) => new(
        "compiler-form-boundary-" + name,
        "-module(form_boundary_contract)." + separator + "-export([run/0])." + separator + "run()->done.",
        Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
    );
}
