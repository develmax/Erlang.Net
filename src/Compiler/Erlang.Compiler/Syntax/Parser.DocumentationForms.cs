namespace Erlang.Compiler;

public sealed partial class Parser
{
    private DocumentationSourceForm ReadDocumentationForm(string name)
    {
        bool parenthesized = Take(ErlangSyntaxTokens.OpenParenthesis);
        Token start = Current;
        Expr expression = Expression();
        if (parenthesized)
        {
            Expect(ErlangSyntaxTokens.CloseParenthesis);
        }
        ExpectFormTerminator();

        if (expression is Expr.Map { Base: null } map && map.Fields.All(field => !field.Exact))
        {
            return ReadDocumentationMap(name, map, start.Start);
        }
        if (expression is Expr.Bits && AttributeLiteral.Normalize(expression, start.Start) is BitString { IsBinary: true } binary)
        {
            return new(name, binary);
        }
        if (expression is Expr.Literal literal && (start.Kind == LexerTokenKinds.String
            || literal.Value is BitString { IsBinary: true } or Atom { Name: "true" or "false" or "hidden" }))
        {
            return new(name, literal.Value);
        }

        throw new CompileException(
            CompilerDiagnosticCodes.UnsupportedSyntax,
            DocumentationAttributeDiagnostics.UnsupportedValue,
            start.Start
        );
    }

    private static DocumentationSourceForm ReadDocumentationMap(string name, Expr.Map map, int offset)
    {
        var values = new Dictionary<Term, Term>();
        Expr.Call? equivalent = null;
        foreach (var field in map.Fields)
        {
            Term key = AttributeLiteral.Normalize(field.Key, offset);
            if (name == ModuleAttributeNames.Doc && key is Atom { Name: "equiv" })
            {
                equivalent = field.Value as Expr.Call;
                if (equivalent is not null)
                {
                    values.Remove(key);
                    continue;
                }
            }
            values[key] = AttributeLiteral.Normalize(field.Value, offset);
        }

        return new(name, new MapTerm(values), equivalent);
    }
}
