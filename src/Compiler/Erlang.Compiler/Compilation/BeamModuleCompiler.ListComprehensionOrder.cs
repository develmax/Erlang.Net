namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private enum ListComprehensionOrder
    {
        Core,
        Ssa,
        Definition
    }

    private static IEnumerable<Expr.ListComprehension> OrderedListComprehensions(
        IReadOnlyList<Clause> clauses,
        ListComprehensionOrder order
    ) => OrderComprehensionClauses(clauses, order)
        .SelectMany(clause => OrderedListComprehensions(clause.Body, order));

    private static IEnumerable<Expr.ListComprehension> OrderedListComprehensions(
        Expr expression,
        ListComprehensionOrder order
    )
    {
        // v3_core:safe_list assigns names right to left, but evaluates operands left to right.
        IEnumerable<Expr> Operands(IEnumerable<Expr> operands) =>
            order == ListComprehensionOrder.Core ? operands.Reverse() : operands;

        IEnumerable<Expr.ListComprehension> In(IEnumerable<Expr> expressions) =>
            expressions.SelectMany(child => OrderedListComprehensions(child, order));

        return expression switch
        {
            Expr.ListComprehension comprehension => [comprehension],
            Expr.Sequence sequence => In(sequence.Items),
            Expr.Block block => OrderedListComprehensions(block.Body, order),
            Expr.Match match => OrderedListComprehensions(match.Value, order),
            Expr.Tuple tuple => In(Operands(tuple.Items)),
            Expr.List or Expr.Call { DynamicRemote: false } => In(Operands(LocalFunctionEntries.Children(expression))),
            Expr.Unary unary => OrderedListComprehensions(unary.Operand, order),
            Expr.Binary { Operator: not ("++" or "andalso" or "orelse") } binary => In(Operands([binary.Left, binary.Right])),
            Expr.Case @case => OrderedListComprehensions(@case.Value, order).Concat(OrderedListComprehensions(@case.Clauses, order)),
            Expr.If @if => OrderedListComprehensions(@if.Clauses, order),
            _ => []
        };
    }

    private static IEnumerable<Clause> OrderComprehensionClauses(
        IReadOnlyList<Clause> clauses,
        ListComprehensionOrder order
    )
    {
        if (clauses.Count < 2 || order == ListComprehensionOrder.Core)
        {
            return clauses;
        }

        int discriminator = -1;
        var literals = new HashSet<Atom>();
        var constructors = new List<Clause>();
        var variables = new List<Clause>();
        foreach (var clause in clauses)
        {
            int column = -1;
            var bindings = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < clause.Patterns.Count; index++)
            {
                if (clause.Patterns[index] is Pattern.Variable variable)
                {
                    if (variable.Name != VariableScopeNames.Wildcard && !bindings.Add(variable.Name))
                    {
                        return [];
                    }
                }
                else if (clause.Patterns[index] is Pattern.Literal { Value: Atom atom }
                    && column < 0 && (discriminator < 0 || discriminator == index) && literals.Add(atom))
                {
                    column = index;
                    discriminator = index;
                }
                else
                {
                    return [];
                }
            }

            if (column < 0)
            {
                variables.Add(clause);
            }
            else if (variables.Count == 0)
            {
                constructors.Add(clause);
            }
            else
            {
                return [];
            }
        }

        if (constructors.Count > 0)
        {
            // match/4 names the default partition first; umatch_list/3 visits selected values right to left.
            if (variables.Count > 1 || variables is [{ Guard: not null }])
            {
                return [];
            }

            return order == ListComprehensionOrder.Ssa
                ? variables.Concat(constructors)
                : constructors.AsEnumerable().Reverse().Concat(variables);
        }

        if (variables.SkipLast(1).Any(clause => clause.Guard is null))
        {
            return [];
        }

        if (order == ListComprehensionOrder.Ssa)
        {
            return variables;
        }

        // umatch visits the group of guarded clauses before visiting the unconditional default.
        return variables[^1].Guard is null
            ? variables.SkipLast(1).Reverse().Append(variables[^1])
            : variables.AsEnumerable().Reverse();
    }
}
