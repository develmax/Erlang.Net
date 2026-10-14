namespace Erlang.Differential;

public static class AtomNameDiagnosticCases
{
    private const int OverMaximumCharacters = 256;
    private const string Prefix = "-module(atom_name_lexer_diagnostic). -export([run/0]). run()->";
    private const string Supplementary = "\U00010000";

    public static IReadOnlyList<CompiledDiagnosticCase> All { get; } =
    [
        Case("quoted", "'" + new string('a', OverMaximumCharacters) + "'", DiagnosticCaseKind.IllegalAtomName),
        Case("bare", new string('a', OverMaximumCharacters), DiagnosticCaseKind.IllegalAtomName),
        Case("variable", new string('A', OverMaximumCharacters), DiagnosticCaseKind.IllegalVariableName),
        Case(
            "quoted-supplementary",
            "'" + string.Concat(Enumerable.Repeat(Supplementary, OverMaximumCharacters)) + "'",
            DiagnosticCaseKind.IllegalAtomName
        )
    ];

    private static CompiledDiagnosticCase Case(string name, string token, DiagnosticCaseKind kind) => new(
        "atom-name-lexer-diagnostic-" + name,
        Prefix + token + ".",
        token,
        Kind: kind
    );
}
