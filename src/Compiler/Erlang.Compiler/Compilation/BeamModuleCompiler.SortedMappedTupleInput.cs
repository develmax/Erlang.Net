namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static bool HasSortedMappedTupleInput(
        Expr source,
        Clause parent,
        Expr.ListComprehension comprehension,
        int arity,
        Atom? tag
    )
    {
        if (source is not Expr.Call
            {
                Module: "lists", Function: "sort", DynamicRemote: false, Arguments: [Expr.Variable input]
            }
            || ReadDominatingComprehensionValue(parent, comprehension, input.Name) is not Expr.ListComprehension
            {
                Items: [Expr.Apply { Function: Expr.Variable callback, Arguments: [Expr.Variable argument] }],
                Qualifiers: [ComprehensionQualifier.Generator { Strict: false, Pattern: Pattern.Variable item }]
            } mapped
            || argument.Name != item.Name || callback.Name == item.Name
            || ReadClosedListClosure(parent, mapped, callback.Name) is not { Clauses: [var mapper] })
        {
            return false;
        }

        // A successful comprehension constructs a proper list. Sorting that list preserves every element.
        return ReturnsRequestedTuple(mapper.Body);

        bool ReturnsRequestedTuple(Expr expr) => expr switch
        {
            Expr.Tuple tuple => tuple.Items.Count == arity
                && (tag is null || tuple.Items[0] is Expr.Literal literal && tag.Equals(literal.Value)),
            Expr.Case decision => decision.Clauses.Count != 0 && decision.Clauses.All(clause => ReturnsRequestedTuple(clause.Body)),
            _ => false
        };
    }

    private static Expr? ReadDominatingComprehensionValue(
        Clause clause,
        Expr.ListComprehension comprehension,
        string name
    )
    {
        if (clause.Body is not Expr.Sequence sequence || ClauseBindsName(clause.Patterns, name))
        {
            return null;
        }

        Expr? binding = null;
        foreach (Expr expression in sequence.Items)
        {
            if (ReferenceEquals(expression, comprehension)
                || expression is Expr.Match { Value: var source } && ReferenceEquals(source, comprehension))
            {
                return binding;
            }

            if (binding is null && expression is Expr.Match { Pattern: Pattern.Variable variable, Value: var value }
                && variable.Name == name)
            {
                binding = value;
            }
        }

        return null;
    }
}
