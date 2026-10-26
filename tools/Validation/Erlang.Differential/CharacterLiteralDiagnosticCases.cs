namespace Erlang.Differential;

public static class CharacterLiteralDiagnosticCases
{
    public static IReadOnlyList<CompiledDiagnosticCase> All { get; } = Build();

    private static IReadOnlyList<CompiledDiagnosticCase> Build()
    {
        var cases = new List<CompiledDiagnosticCase>();
        int index = 0;
        foreach (string literal in new[]
        {
            """$\xA""", """$\xAG""", """$\x{}""", """$\x{G}""",
            """$\x{d800}""", """$\x{fffe}""", """$\x{ffff}""", """$\x{110000}""",
            """$\^0""", """$\^`""", """$\^é""", """$\x{ffffffffffffffff}"""
        })
            cases.Add(new(
                "character-literal-illegal-" + index++,
                "-module(character_literal_bad). -export([run/0]). run()->" + literal,
                "character",
                Kind: DiagnosticCaseKind.CharacterLiteralIllegal
            ));
        index = 0;
        foreach (string literal in new[] { "$", "$\\", """$\x""", """$\^""", """$\x{41""" })
            cases.Add(new(
                "character-literal-unterminated-" + index++,
                "-module(character_literal_bad). -export([run/0]). run()->" + literal,
                "char",
                Kind: DiagnosticCaseKind.CharacterLiteralUnterminated
            ));

        return cases;
    }
}
