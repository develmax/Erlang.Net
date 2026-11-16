namespace Erlang.Compiler;

internal static class BeamMacroArguments
{
    private const string PendingAnonymousFun = "fun_end";

    internal static List<Token[]> Read(IReadOnlyList<Token> tokens, ref int index, bool trackExpressionBlocks = true)
    {
        var arguments = new List<Token[]>();
        var nested = new Stack<string>();
        int start = index + 1;
        for (int cursor = start; cursor < tokens.Count; cursor++)
        {
            var token = tokens[cursor];
            if (token.Kind is not (LexerTokenKinds.Symbol or LexerTokenKinds.Keyword))
            {
                continue;
            }
            if (nested.TryPeek(out var pending) && pending == PendingAnonymousFun)
            {
                if (token.Text is ErlangSyntaxTokens.Comma or ErlangSyntaxTokens.CloseParenthesis)
                {
                    nested.Pop();
                }
                else if (token.Text is ErlangKeywords.When or ErlangSyntaxTokens.FunctionArrow)
                {
                    nested.Pop();
                    nested.Push(ErlangKeywords.End);
                }
            }
            if (nested.Count == 0 && token.Text is ErlangSyntaxTokens.Comma or ErlangSyntaxTokens.CloseParenthesis)
            {
                if (cursor > start)
                {
                    var argument = new Token[cursor - start];
                    for (int offset = 0; offset < argument.Length; offset++)
                    {
                        argument[offset] = tokens[start + offset];
                    }
                    arguments.Add(argument);
                }
                else if (arguments.Count != 0 || token.Text == ErlangSyntaxTokens.Comma)
                {
                    break;
                }
                if (token.Text == ErlangSyntaxTokens.CloseParenthesis)
                {
                    index = cursor;

                    return arguments;
                }
                start = cursor + 1;
                continue;
            }

            string? closing = ClosingToken(tokens, cursor, trackExpressionBlocks);
            if (closing is not null)
            {
                nested.Push(closing);
            }
            else if (nested.TryPeek(out string? expected) && token.Text == expected)
            {
                nested.Pop();
            }
        }

        throw new CompileException(CompilerDiagnosticCodes.Syntax, BeamMacroDiagnostics.InvalidArguments, tokens[index].Start);
    }

    private static string? ClosingToken(IReadOnlyList<Token> tokens, int index, bool trackExpressionBlocks) => tokens[index].Text switch
    {
        ErlangSyntaxTokens.OpenParenthesis => ErlangSyntaxTokens.CloseParenthesis,
        ErlangSyntaxTokens.OpenTuple => ErlangSyntaxTokens.CloseTuple,
        ErlangSyntaxTokens.OpenList => ErlangSyntaxTokens.CloseList,
        ErlangSyntaxTokens.BinaryOpen => ErlangSyntaxTokens.BinaryClose,
        _ when !trackExpressionBlocks => null,
        ErlangKeywords.Fun when OpensParenthesis(tokens, index + 1) => PendingAnonymousFun,
        ErlangKeywords.Fun when index + 1 < tokens.Count && tokens[index + 1].Kind == LexerTokenKinds.Variable
            && OpensParenthesis(tokens, index + 2) => ErlangKeywords.End,
        ErlangKeywords.Begin or ErlangKeywords.Case or ErlangKeywords.If or ErlangKeywords.Receive
            or ErlangKeywords.Try or ErlangKeywords.Maybe or ErlangKeywords.Cond => ErlangKeywords.End,
        _ => null
    };

    private static bool OpensParenthesis(IReadOnlyList<Token> tokens, int index) =>
        index < tokens.Count && tokens[index] is { Kind: LexerTokenKinds.Symbol, Text: ErlangSyntaxTokens.OpenParenthesis };
}
