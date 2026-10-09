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
        _ => throw new NotSupportedException("Literal emission is not supported for " + term.GetType().Name)
    };
    private static string ListLiteral(Cons c)
    { var items = new List<Term>(); Term tail = c; while (tail is Cons cell) { items.Add(cell.Head); tail = cell.Tail; } return "global::Erlang.Cons.From(" + Array(items, TermCode, "global::Erlang.Term") + "," + TermCode(tail) + ")"; }
    private static string PatternCode(Pattern p) => p switch
    {
        Pattern.Any => "new " + P + "Any()",
        Pattern.Variable v => "new " + P + "Variable(" + Quote(v.Name) + ")",
        Pattern.Literal l => "new " + P + "Literal(" + TermCode(l.Value) + ")",
        Pattern.Tuple t => "new " + P + "Tuple(" + Array(t.Items, PatternCode, "global::Erlang.Pattern") + ")",
        Pattern.List l => "new " + P + "List(" + Array(l.Items, PatternCode, "global::Erlang.Pattern") + "," + (l.Tail is null ? "null" : PatternCode(l.Tail)) + ")",
        _ => throw new NotSupportedException()
    };
    private static string ClauseCode(Clause c) => "new global::Erlang.Compiler.Clause(" + Array(c.Patterns, PatternCode, "global::Erlang.Pattern") + "," + Optional(c.Guard) + "," + ExpressionCode(c.Body) + ")";
    private static string Clauses(IReadOnlyList<Clause> clauses) => Array(clauses, ClauseCode, "global::Erlang.Compiler.Clause");
    private static string Expressions(IReadOnlyList<Expr> expressions) => Array(expressions, ExpressionCode, "global::Erlang.Compiler.Expr");
    private static string Optional(Expr? expression) => expression is null ? "null" : ExpressionCode(expression);
    public static string ExpressionCode(Expr expression) => expression switch
    {
        Expr.Literal l => "new " + E + "Literal(" + TermCode(l.Value) + ")",
        Expr.Variable v => "new " + E + "Variable(" + Quote(v.Name) + ")",
        Expr.Tuple t => "new " + E + "Tuple(" + Expressions(t.Items) + ")",
        Expr.List l => "new " + E + "List(" + Expressions(l.Items) + "," + Optional(l.Tail) + ")",
        Expr.Unary u => "new " + E + "Unary(" + Quote(u.Operator) + "," + ExpressionCode(u.Operand) + ")",
        Expr.Binary b => "new " + E + "Binary(" + Quote(b.Operator) + "," + ExpressionCode(b.Left) + "," + ExpressionCode(b.Right) + ")",
        Expr.Call c => "new " + E + "Call(" + (c.Module is null ? "null" : Quote(c.Module)) + "," + Quote(c.Function) + "," + Expressions(c.Arguments) + ")",
        Expr.Apply a => "new " + E + "Apply(" + ExpressionCode(a.Function) + "," + Expressions(a.Arguments) + ")",
        Expr.Match m => "new " + E + "Match(" + PatternCode(m.Pattern) + "," + ExpressionCode(m.Value) + ")",
        Expr.Sequence s => "new " + E + "Sequence(" + Expressions(s.Items) + ")",
        Expr.GuardAlternatives s => "new " + E + "GuardAlternatives(" + Expressions(s.Items) + ")",
        Expr.Case c => "new " + E + "Case(" + ExpressionCode(c.Value) + "," + Clauses(c.Clauses) + ")",
        Expr.Receive r => "new " + E + "Receive(" + Clauses(r.Clauses) + "," + Optional(r.Timeout) + "," + Optional(r.After) + ")",
        Expr.Fun f => "new " + E + "Fun(" + Clauses(f.Clauses) + ")",
        _ => throw new NotSupportedException()
    };
    public static string CompileModule(string source, string sourcePath)
    {
        var module = new Parser(source).ParseModule();
        string className = "ErlangModule_" + string.Concat(module.Name.Select(c => char.IsAsciiLetterOrDigit(c) || c == '_' ? c.ToString() : "_" + ((int)c).ToString("x4", CultureInfo.InvariantCulture)));
        return "// Generated from " + sourcePath.Replace("\n", " ") + "\n#nullable enable\nnamespace Erlang.Generated;\npublic static class " + className + "\n{\npublic static global::Erlang.Compiler.ModuleDefinition Definition {get;} = new(" + Quote(module.Name) + ",new (string Name,int Arity)[]{" + string.Join(',', module.Exports.Select(x => "(" + Quote(x.Name) + "," + x.Arity + ")")) + "},new global::Erlang.Compiler.FunctionDefinition[]{" + string.Join(',', module.Functions.Select(f => "new global::Erlang.Compiler.FunctionDefinition(" + Quote(f.Name) + "," + f.Arity + "," + Clauses(f.Clauses) + ")")) + "});\npublic static void Register(global::Erlang.ModuleRegistry registry)=>Definition.Register(registry);\n}\n";
    }
    public static string Preprocess(string source, string path, string nullableContext = "enable")
    {
        // Roslyn supplies C# lexical boundaries, including comments, raw strings and interpolation.
        var tokens = SyntaxFactory.ParseTokens(source).Where(t => !t.IsKind(SyntaxKind.EndOfFileToken)).ToArray();
        var result = new StringBuilder(); int copied = 0;
        for (int i = 0; i < tokens.Length; i++)
        {
            var token = tokens[i]; if (token.SpanStart < copied) continue;
            if (token.Text is not ("receive" or "case" or "fun")) continue;
            if (i == 0 || tokens[i - 1].Text is not ("=" or "{" or ";" or "return" or "=>")) continue;
            if (i + 1 >= tokens.Length || tokens[i + 1].Text is "." or ";" or "=" or ":" or ",") continue;
            // An ordinary C# call to a method named fun/receive must remain C#.
            if (token.Text == "receive" && tokens[i + 1].Text == "(") continue;
            if (token.Text == "fun")
            {
                int depth = 0, close = -1; for (int n = i + 1; n < tokens.Length; n++) { if (tokens[n].Text == "(") depth++; if (tokens[n].Text == ")" && --depth == 0) { close = n; break; } }
                if (close < 0 || close + 1 >= tokens.Length || tokens[close + 1].Text is not ("->" or "when")) continue;
            }
            if (token.Text == "case")
            {
                bool hasOf = false; int depth = 0;
                for (int n = i + 1; n < tokens.Length; n++)
                { string text = tokens[n].Text; if (depth == 0 && text is ";" or "}" or "->") break; if (depth == 0 && text == "of" && n > i + 1) { hasOf = true; break; } if (text is "(" or "[" or "{") depth++; if (text is ")" or "]" or "}") depth--; }
                if (!hasOf) continue;
            }
            int start = token.SpanStart;
            var parser = new Parser(source[start..], true); Expr expression;
            try { expression = parser.ParseExpression(); Semantics.Validate(expression); }
            catch (CompileException ex) { throw new CompileException(ex.Code, ex.Message, start + ex.Offset); }
            int end = start + parser.EndOffset; while (end < source.Length && char.IsWhiteSpace(source[end])) end++;
            if (end >= source.Length || source[end] != '.') throw new CompileException("ERL008", "Embedded Erlang expressions must end with 'end.'", end);
            end++;
            result.Append(source[copied..start]);
            result.Append("await global::Erlang.Compiler.Execution.EvaluateAsync(").Append(ExpressionCode(expression)).Append(", erlangProcess)");
            // '.' terminates an Erlang expression and maps to ';' for a C# statement/assignment.
            result.Append(';'); int newlines = source[start..end].Count(c => c == '\n'); result.Append('\n', newlines); copied = end;
        }
        result.Append(source[copied..]);
        string nullable = nullableContext switch { "enable" => "#nullable enable\n", "annotations" => "#nullable disable\n#nullable enable annotations\n", "warnings" => "#nullable disable\n#nullable enable warnings\n", _ => "#nullable disable\n" };
        return nullable + "#line 1 " + Quote(Path.GetFullPath(path)) + "\n" + result;
    }
}
