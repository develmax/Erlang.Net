namespace Erlang.Compiler;

public sealed partial class Parser
{
    private (string Module, Term Signatures) ReadImportArguments()
    {
        int offset = Current.Start;
        string module = Name();
        Expect(ErlangSyntaxTokens.Comma);
        Term signatures = AttributeLiteral.Normalize(Expression(), offset);
        Expect(ErlangSyntaxTokens.CloseParenthesis);
        ExpectFormTerminator();

        return (module, signatures);
    }

    internal static Term ReadReleaseImport(string source, IReadOnlyList<Token> tokens, int start)
    {
        var parser = new Parser(source, tokens as List<Token> ?? [.. tokens]) { position = start };
        parser.Expect(ErlangSyntaxTokens.OpenParenthesis);
        var (module, signatures) = parser.ReadImportArguments();

        return Term.Tuple(Term.A(module), signatures);
    }
}
