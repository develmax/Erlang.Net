using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Erlang.Compiler;

public static class CodeGeneration
{
    private const string E = "global::Erlang.Compiler.Expr.";
    private const string P = "global::Erlang.Pattern.";

    private static string Quote(string text) => JsonSerializer.Serialize(text);

    private static string Array<T>(IEnumerable<T> items, Func<T, string> emit, string type) => "new " + type + "[]{" + string.Join(',', items.Select(emit)) + "}";

    public static string TermCode(Term term) => term switch
    {
        Atom a => "global::Erlang.Term.A(" + Quote(a.Name) + ")",
        Integer i => "new global::Erlang.Integer(global::System.Numerics.BigInteger.Parse(" + Quote(i.ToString()) + ",global::System.Globalization.CultureInfo.InvariantCulture))",
        FloatTerm f => "new global::Erlang.FloatTerm(" + f.Value.ToString("R", CultureInfo.InvariantCulture) + "d)",
        Nil => "global::Erlang.Nil.Value",
        Cons c => ListLiteral(c),
        TupleTerm t => "new global::Erlang.TupleTerm(" + Array(t.Items, TermCode, "global::Erlang.Term") + ")",
        MapTerm m => "new global::Erlang.MapTerm(" + Array(
            m.Entries,
            f => "new global::System.Collections.Generic.KeyValuePair<global::Erlang.Term,global::Erlang.Term>(" + TermCode(f.Key) + "," + TermCode(f.Value) + ")",
            "global::System.Collections.Generic.KeyValuePair<global::Erlang.Term,global::Erlang.Term>"
        ) + ")",
        BitString bits => "new global::Erlang.BitString(new byte[]{" + string.Join(',', bits.ToArray()) + "}," + bits.BitLength + ")",
        _ => throw new NotSupportedException(CodeGenerationDiagnostics.UnsupportedLiteral(term.GetType().Name))
    };

    private static string ListLiteral(Cons c)
    {
        var items = new List<Term>();
        Term tail = c;
        while (tail is Cons cell)
        {
            items.Add(cell.Head);
            tail = cell.Tail;
        }

        return "global::Erlang.Cons.From(" + Array(items, TermCode, "global::Erlang.Term") + "," + TermCode(tail) + ")";
    }

    private static string PatternCode(Pattern p) => p switch
    {
        Pattern.Any => "new " + P + "Any()",
        Pattern.Variable v => "new " + P + "Variable(" + Quote(v.Name) + ")",
        Pattern.Literal l => "new " + P + "Literal(" + TermCode(l.Value) + ")",
        Pattern.Tuple t => "new " + P + "Tuple(" + Array(t.Items, PatternCode, "global::Erlang.Pattern") + ")",
        Pattern.List l => "new " + P + "List(" + Array(l.Items, PatternCode, "global::Erlang.Pattern") + "," + (l.Tail is null ? "null" : PatternCode(l.Tail)) + ")",
        MapPattern m => "new global::Erlang.Compiler.MapPattern(" + Array(
            m.Fields,
            f => "new global::Erlang.Compiler.MapPatternField(" + ExpressionCode(f.Key) + "," + PatternCode(f.Value) + ")",
            "global::Erlang.Compiler.MapPatternField"
        ) + ")",
        BitPattern bits => "new global::Erlang.Compiler.BitPattern(" + Array(
            bits.Segments,
            s => "new global::Erlang.Compiler.BitPatternSegment(" + PatternCode(s.Value) + "," + BitSegmentCode(s.Specification) + ")",
            "global::Erlang.Compiler.BitPatternSegment"
        ) + ")",
        _ => throw new NotSupportedException()
    };

    private static string ClauseCode(Clause c) => "new global::Erlang.Compiler.Clause(" + Array(c.Patterns, PatternCode, "global::Erlang.Pattern") + "," + Optional(c.Guard) + "," + ExpressionCode(c.Body) + ")";

    private static string Clauses(IReadOnlyList<Clause> clauses) => Array(clauses, ClauseCode, "global::Erlang.Compiler.Clause");

    private static string Expressions(IReadOnlyList<Expr> expressions) => Array(expressions, ExpressionCode, "global::Erlang.Compiler.Expr");

    private static string Optional(Expr? expression) => expression is null ? "null" : ExpressionCode(expression);

    private static string BitSegmentCode(BitSegment s) => "new global::Erlang.Compiler.BitSegment(" + ExpressionCode(s.Value) + "," + Optional(s.Size) + "," + Quote(s.Type) + "," + s.Unit + "," + Quote(s.Endian) + "," + (s.Signed ? "true" : "false") + "," + (s.IsStringLiteral ? "true" : "false") + ")";

    public static string ExpressionCode(Expr expression) => expression switch
    {
        Expr.Literal l => "new " + E + "Literal(" + TermCode(l.Value) + ")",
        Expr.Variable v => "new " + E + "Variable(" + Quote(v.Name) + ")",
        Expr.Tuple t => "new " + E + "Tuple(" + Expressions(t.Items) + ")",
        Expr.List l => "new " + E + "List(" + Expressions(l.Items) + "," + Optional(l.Tail) + ")",
        Expr.Map m => "new " + E + "Map(" + Optional(m.Base) + "," + Array(
            m.Fields,
            f => "new global::Erlang.Compiler.MapField(" + ExpressionCode(f.Key) + "," + ExpressionCode(f.Value) + "," + (f.Exact ? "true" : "false") + ")",
            "global::Erlang.Compiler.MapField"
        ) + ")",
        Expr.Bits bits => "new " + E + "Bits(" + Array(bits.Segments, BitSegmentCode, "global::Erlang.Compiler.BitSegment") + ")",
        Expr.Unary u => "new " + E + "Unary(" + Quote(u.Operator) + "," + ExpressionCode(u.Operand) + ")",
        Expr.Binary b => "new " + E + "Binary(" + Quote(b.Operator) + "," + ExpressionCode(b.Left) + "," + ExpressionCode(b.Right) + ")",
        Expr.Call c => "new " + E + "Call(" + (c.Module is null ? "null" : Quote(c.Module)) + "," + Quote(c.Function) + "," + Expressions(c.Arguments) + ")",
        Expr.Apply a => "new " + E + "Apply(" + ExpressionCode(a.Function) + "," + Expressions(a.Arguments) + ")",
        Expr.Match m => "new " + E + "Match(" + PatternCode(m.Pattern) + "," + ExpressionCode(m.Value) + ")",
        Expr.Sequence s => "new " + E + "Sequence(" + Expressions(s.Items) + ")",
        Expr.Block block => "new " + E + "Block(" + ExpressionCode(block.Body) + ")",
        Expr.Catch caught => "new " + E + "Catch(" + ExpressionCode(caught.Operand) + ")",
        Expr.Try tried => "new " + E + "Try(" + ExpressionCode(tried.Body) + "," + Clauses(tried.Clauses) + "," + Clauses(tried.Catches) + "," + Optional(tried.After) + ")",
        Expr.GuardAlternatives s => "new " + E + "GuardAlternatives(" + Expressions(s.Items) + ")",
        Expr.Case c => "new " + E + "Case(" + ExpressionCode(c.Value) + "," + Clauses(c.Clauses) + ")",
        Expr.If i => "new " + E + "If(" + Clauses(i.Clauses) + ")",
        Expr.Receive r => "new " + E + "Receive(" + Clauses(r.Clauses) + "," + Optional(r.Timeout) + "," + Optional(r.After) + ")",
        Expr.Fun f => "new " + E + "Fun(" + Clauses(f.Clauses) + ")",
        _ => throw new NotSupportedException()
    };

    public static string CompileModule(string source, string sourcePath)
    {
        var module = new Parser(source).ParseModule();
        string className = "ErlangModule_" + string.Concat(module.Name.Select(c => char.IsAsciiLetterOrDigit(c) || c == '_' ? c.ToString() : "_" + ((int)c).ToString("x4", CultureInfo.InvariantCulture)));

        return "// Generated from " + sourcePath.Replace("\n", " ") + "\n#nullable enable\nnamespace Erlang.Generated;\npublic static class " + className + "\n{\npublic static global::Erlang.Compiler.ModuleDefinition Definition {get;} = new(" + Quote(module.Name) + ",new (string Name,int Arity)[]{" + string.Join(',', module.Exports.Select(x => "(" + Quote(x.Name) + "," + x.Arity + ")")) + "},new global::Erlang.Compiler.FunctionDefinition[]{" + string.Join(
            ',',
            module.Functions.Select(f => "new global::Erlang.Compiler.FunctionDefinition(" + Quote(f.Name) + "," + f.Arity + "," + Clauses(f.Clauses) + ")")
        ) + "});\npublic static void Register(global::Erlang.ModuleRegistry registry)=>Definition.Register(registry);\n}\n";
    }

    public static string Preprocess(string source, string path, string nullableContext = "enable")
    {
        // Roslyn supplies C# lexical boundaries, including comments, raw strings and interpolation.
        var tokens = SyntaxFactory.ParseTokens(source).Where(t => !t.IsKind(SyntaxKind.EndOfFileToken)).ToArray();
        var result = new StringBuilder();
        int copied = 0;
        for (int i = 0; i < tokens.Length; i++)
        {
            var token = tokens[i];
            if (token.SpanStart < copied)
                continue;
            if (token.Text is not (ErlangKeywords.Receive or ErlangKeywords.Case or ErlangKeywords.Fun or ErlangKeywords.If or ErlangKeywords.Begin or ErlangKeywords.Catch or ErlangKeywords.Try))
                continue;
            if (i == 0 || tokens[i - 1].Text is not ("=" or "{" or ";" or "return" or "=>"))
                continue;
            int following = token.Span.End;
            while (following < source.Length && char.IsWhiteSpace(source[following]))
                following++;
            if (following == source.Length || source[following] is '.' or ';' or '=' or ':' or ',')
                continue;
            if (token.Text == ErlangKeywords.If && !HasIfGuard(source[token.SpanStart..]))
                continue;
            if (token.Text is ErlangKeywords.Begin or ErlangKeywords.Catch or ErlangKeywords.Try && !HasBlockExpression(source[token.SpanStart..]))
                continue;
            // An ordinary C# call to a method named fun/receive must remain C#.
            if (token.Text == "receive" && source[following] == '(')
                continue;
            if (token.Text == "fun")
            {
                int depth = 0, close = -1;
                for (int n = i + 1; n < tokens.Length; n++)
                {
                    if (tokens[n].Text == "(")
                        depth++;
                    if (tokens[n].Text == ")" && --depth == 0)
                    {
                        close = n;
                        break;
                    }
                }
                if (close < 0 || close + 1 >= tokens.Length || tokens[close + 1].Text is not ("->" or "when"))
                    continue;
            }
            if (token.Text == "case")
            {
                bool hasOf = false;
                int depth = 0;
                // Roslyn treats Erlang '#' as directive trivia, hiding the map and 'of'.
                // Inspect this candidate with the Erlang lexer while retaining Roslyn's C# start boundary.
                List<Token> probe;
                try
                {
                    probe = Lexer.Scan(source[token.SpanStart..], true);
                }
                catch (CompileException)
                {
                    continue;
                }
                for (int n = 1; n < probe.Count; n++)
                {
                    string text = probe[n].Text;
                    if (probe[n].Kind is LexerTokenKinds.QuotedAtom or LexerTokenKinds.String)
                        continue;
                    if (depth == 0 && text is ";" or "}" or "->")
                        break;
                    if (depth == 0 && text == "of" && n > 1)
                    {
                        hasOf = true;
                        break;
                    }
                    if (text is "(" or "[" or "{")
                        depth++;
                    if (text is ")" or "]" or "}")
                        depth--;
                }
                if (!hasOf)
                    continue;
            }
            int start = token.SpanStart;
            var parser = new Parser(source[start..], true);
            Expr expression;
            try
            {
                expression = parser.ParseExpression();
                Semantics.Validate(expression);
            }
            catch (CompileException ex)
            {
                throw new CompileException(ex.Code, ex.Message, start + ex.Offset);
            }
            int end = start + parser.EndOffset;
            while (end < source.Length && char.IsWhiteSpace(source[end]))
                end++;
            if (end >= source.Length || source[end] != '.')
                throw new CompileException(CompilerDiagnosticCodes.EmbeddedExpression, HybridDiagnostics.MissingExpressionTerminator, end);
            end++;
            result.Append(source[copied..start]);
            result.Append("await global::Erlang.Compiler.Execution.EvaluateAsync(").Append(ExpressionCode(expression)).Append(", erlangProcess)");
            // '.' terminates an Erlang expression and maps to ';' for a C# statement/assignment.
            result.Append(';');
            int newlines = source[start..end].Count(c => c == '\n');
            result.Append('\n', newlines);
            copied = end;
        }
        result.Append(source[copied..]);
        string nullable = nullableContext switch
        {
            "enable" => "#nullable enable\n",
            "annotations" => "#nullable disable\n#nullable enable annotations\n",
            "warnings" => "#nullable disable\n#nullable enable warnings\n",
            _ => "#nullable disable\n"
        };

        return nullable + "#line 1 " + Quote(Path.GetFullPath(path)) + "\n" + result;
    }

    private static bool HasIfGuard(string source)
    {
        try
        {
            return new Parser(source, true).ParseExpression() is Expr.If;
        }
        catch (CompileException)
        {
            return false;
        }
    }

    private static bool HasBlockExpression(string source)
    {
        try
        {
            return new Parser(source, true).ParseExpression() is Expr.Block or Expr.Catch or Expr.Try;
        }
        catch (CompileException)
        {
            return false;
        }
    }
}
