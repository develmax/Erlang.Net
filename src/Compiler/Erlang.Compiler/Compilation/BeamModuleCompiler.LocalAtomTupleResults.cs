namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private const int LocalAtomOrTupleType = AtomTypeBits | TupleTypeBits;

    private int? ReadLocalAtomTupleClauses(IReadOnlyList<Clause> clauses)
    {
        int result = 0;
        foreach (Clause clause in clauses)
        {
            if (ReadLocalAtomTupleResult(clause.Body) is not int type)
            {
                return null;
            }

            result |= type;
        }

        return result;
    }

    private int? ReadLocalAtomTupleResult(Expr expression) => expression switch
    {
        Expr.Literal { Value: Atom } => AtomTypeBits,
        Expr.Tuple or Expr.Literal { Value: TupleTerm } => TupleTypeBits,
        Expr.Sequence { Items.Count: > 0 } sequence => ReadLocalAtomTupleResult(sequence.Items[^1]),
        Expr.Block block => ReadLocalAtomTupleResult(block.Body),
        Expr.Match match => ReadLocalAtomTupleResult(match.Value),
        Expr.Case decision => ReadLocalAtomTupleClauses(decision.Clauses),
        Expr.If decision => ReadLocalAtomTupleClauses(decision.Clauses),
        Expr.Call call when IsKnownNonreturningLocalResult(call) => 0,
        _ => null
    };
}
