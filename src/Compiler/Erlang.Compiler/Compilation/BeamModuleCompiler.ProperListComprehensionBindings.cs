namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static bool HasProperListBinding(Clause clause, Expr.ListComprehension comprehension, string name) =>
        HasDominatingListBinding(
            clause,
            comprehension,
            name,
            IsProperListValue
        );

    private static bool HasDominatingListBinding(
        Clause clause,
        Expr.ListComprehension comprehension,
        string name,
        Func<Expr, HashSet<string>, bool> accepts,
        bool walkAppendConsumer = false
    )
    {
        return name != VariableScopeNames.Wildcard && Visit(clause.Body, new HashSet<string>(StringComparer.Ordinal)) == true;

        bool? Visit(Expr expression, HashSet<string> inherited)
        {
            if (ReferenceEquals(expression, comprehension))
            {
                return inherited.Contains(name);
            }

            switch (expression)
            {
                case Expr.Sequence sequence:
                    // The target inherits only the facts established on its own path.
                    var proper = new HashSet<string>(inherited, StringComparer.Ordinal);
                    foreach (Expr item in sequence.Items)
                    {
                        if (Visit(item, proper) is bool result)
                        {
                            return result;
                        }

                        if (item is Expr.Match { Pattern: Pattern.Variable bound, Value: var value }
                            && bound.Name != VariableScopeNames.Wildcard && accepts(value, proper))
                        {
                            // A match against a bound name also establishes the type on success.
                            proper.Add(bound.Name);
                        }
                    }

                    break;
                case Expr.Match match:
                    return Visit(match.Value, inherited);
                case Expr.Case decision:
                    if (Visit(decision.Value, inherited) is bool selectorResult)
                    {
                        return selectorResult;
                    }

                    // Case patterns check existing bindings. Each arm has its own
                    // assignments, and nothing from an arm is joined here.
                    foreach (Clause arm in decision.Clauses)
                    {
                        if (Visit(arm.Body, inherited) is bool armResult)
                        {
                            return armResult;
                        }
                    }

                    break;
                case Expr.Block block:
                    return Visit(block.Body, inherited);
                case Expr.Call { DynamicRemote: false, Arguments: [var argument] } when walkAppendConsumer:
                    return Visit(argument, inherited);
                case Expr.Binary { Operator: ErlangOperators.Append, Left: Expr.ListComprehension left }
                    when walkAppendConsumer:
                    return Visit(left, inherited);
            }

            return null;
        }

    }

    private static bool IsProperListValue(Expr value, HashSet<string> proper) => value switch
    {
        Expr.Literal { Value: Nil } => true,
        Expr.Variable bound => proper.Contains(bound.Name),
        Expr.List { Tail: null or Expr.Literal { Value: Nil } } => true,
        Expr.List { Tail: Expr.Variable tail } => proper.Contains(tail.Name),
        Expr.ListComprehension => true,
        _ => false
    };
}
