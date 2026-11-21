namespace Erlang.Compiler;

public sealed partial class Parser
{
    internal IReadOnlyList<Term> ParseAssemblyTerms()
    {
        var forms = new List<Term>();
        while (Current.Kind != LexerTokenKinds.EndOfInput)
        {
            int offset = Current.Start;
            Expr term = Expression();
            if (!IsAssemblyTerm(term))
            {
                throw BeamAssemblyDiagnostics.InvalidTerm(offset);
            }

            forms.Add(AttributeLiteral.Normalize(term, offset));
            ExpectFormTerminator();
        }

        return forms;
    }

    private static bool IsAssemblyTerm(Expr term) => term switch
    {
        Expr.Literal => true,
        Expr.Tuple tuple => tuple.Items.All(IsAssemblyTerm),
        Expr.List list => list.Items.All(IsAssemblyTerm) && (list.Tail is null || IsAssemblyTerm(list.Tail)),
        Expr.Map { Base: null } map => map.Fields.All(field => !field.Exact && IsAssemblyTerm(field.Key) && IsAssemblyTerm(field.Value)),
        Expr.Bits bits => bits.Segments.All(segment => IsAssemblyTerm(segment.Value) && (segment.Size is null || IsAssemblyTerm(segment.Size))),
        Expr.Unary { Operator: ErlangOperators.Minus or ErlangOperators.Plus, Operand: Expr.Literal { Value: Integer or FloatTerm } } => true,
        _ => false
    };
}
