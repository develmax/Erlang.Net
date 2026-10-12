using Erlang.Compiler;

namespace Erlang.Tests;

public static class CharacterLiteralScannerContracts
{
    public static void Run()
    {
        var tokens = Lexer.Scan("$😀+1");
        if (tokens[0] is not { Kind: CharacterLiteralScannerExpectations.IntegerTokenKind, Text: "128512", Start: 0, End: 3 } || tokens[1].Start != 3)
            throw new InvalidOperationException(CharacterLiteralScannerDiagnostics.SourceOffsets);

        foreach (var (source, first, second) in new[]
        {
            ("""$\7777""", "511", "7"),
            ("""$\78""", "7", "8"),
            ("""$\xAf0""", "175", "0")
        })
        {
            tokens = Lexer.Scan(source);
            if (tokens[0].Text != first || tokens[1].Text != second)
                throw new InvalidOperationException(CharacterLiteralScannerDiagnostics.FollowingToken);
        }

        foreach (string source in new[]
        {
            "$", "$\\", "$\ud800", "$\udfff", "$\ufffe", "$\uffff",
            """$\x""", """$\xA""", """$\xAG""", """$\x{}""", """$\x{41""", """$\x{G}""",
            """$\x{d800}""", """$\x{ffff}""", """$\x{110000}""", """$\x{ffffffffffffffff}""",
            """$\^""", """$\^0""", """$\^`""", """$\^é"""
        })
        {
            try
            {
                Lexer.Scan(source);
            }
            catch (CompileException exception) when (exception.Code == CharacterLiteralScannerExpectations.InvalidLiteralCode && exception.Offset == 0)
            {
                continue;
            }

            throw new InvalidOperationException(CharacterLiteralScannerDiagnostics.MalformedLiteral);
        }

        try
        {
            Lexer.Scan("42 + $\\x{110000}");
        }
        catch (CompileException exception) when (exception.Code == CharacterLiteralScannerExpectations.InvalidLiteralCode && exception.Offset == 5)
        {
            return;
        }

        throw new InvalidOperationException(CharacterLiteralScannerDiagnostics.DollarOffset);
    }
}
