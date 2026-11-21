namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private enum ListComprehensionOrder
    {
        Core,
        Ssa,
        Definition
    }

    private IEnumerable<Expr> OrderedLocalDefinitions(
        IReadOnlyList<Clause> clauses,
        ListComprehensionOrder order,
        IReadOnlySet<string>? outerBindings = null,
        bool functionHead = false
    )
    {
        var ordered = outerBindings is null ? OrderComprehensionClauses(clauses, order)
            : OrderConstructorComprehensionClauses(
                clauses,
                order,
                outerBindings,
                functionHead: functionHead
            );
        foreach (var clause in ordered)
        {
            IReadOnlySet<string>? bindings = null;
            if (outerBindings is not null)
            {
                var bound = new HashSet<string>(outerBindings, StringComparer.Ordinal);
                bound.UnionWith(clause.Patterns.SelectMany(Semantics.Variables));
                bindings = bound;
            }

            foreach (var definition in OrderedLocalDefinitions(clause.Body, order, bindings))
            {
                yield return definition;
            }
        }
    }

    private IEnumerable<Expr> OrderedLocalDefinitions(
        Expr expression,
        ListComprehensionOrder order,
        IReadOnlySet<string>? outerBindings = null
    )
    {
        // v3_core:safe_list assigns names right to left, but evaluates operands left to right.
        IEnumerable<Expr> Operands(IEnumerable<Expr> operands) =>
            order == ListComprehensionOrder.Core ? operands.Reverse() : operands;

        IEnumerable<Expr> In(IEnumerable<Expr> expressions) =>
            expressions.SelectMany(child => OrderedLocalDefinitions(child, order, outerBindings));

        IEnumerable<Expr> Sequence(IReadOnlyList<Expr> items)
        {
            if (outerBindings is null)
            {
                foreach (var definition in In(items))
                {
                    yield return definition;
                }

                yield break;
            }

            var current = new HashSet<string>(outerBindings, StringComparer.Ordinal);
            foreach (var item in items)
            {
                foreach (var definition in OrderedLocalDefinitions(item, order, current))
                {
                    yield return definition;
                }

                if (item is Expr.Match match)
                {
                    current.UnionWith(Semantics.Variables(match.Pattern));
                }
            }
        }

        return expression switch
        {
            Expr.ListComprehension or Expr.Fun => [expression],
            Expr.Sequence sequence => Sequence(sequence.Items),
            Expr.Block block => OrderedLocalDefinitions(block.Body, order, outerBindings),
            Expr.Match match => OrderedLocalDefinitions(match.Value, order, outerBindings),
            Expr.Tuple tuple => In(Operands(tuple.Items)),
            // Tuple records use declaration order. Leave defaults without editable occurrences to the fallback.
            Expr.RecordConstruction { Wildcard: null } record => In(Operands(record.Definition.Fields
                .Select(field => record.Fields.FirstOrDefault(initializer => initializer.Name == field.Name)?.Value)
                .OfType<Expr>())),
            Expr.List or Expr.Call { DynamicRemote: false } => In(Operands(LocalFunctionEntries.Children(expression))),
            Expr.Unary unary => OrderedLocalDefinitions(unary.Operand, order, outerBindings),
            // Only a direct list comprehension on the left uses v3_core's append fusion.
            Expr.Binary { Operator: ErlangOperators.Append, Left: not Expr.ListComprehension } append =>
                In(Operands([append.Left, append.Right])),
            Expr.Binary { Operator: not ("++" or "andalso" or "orelse") } binary => In(Operands([binary.Left, binary.Right])),
            // Core folds a literal selector before allocating SSA helper ordinals.
            Expr.Case @case when outerBindings is not null && AttributeLiteral.TryNormalizeConstant(@case.Value, out _) =>
                OrderedLocalDefinitions(@case.Value, order, outerBindings),
            Expr.Case @case => OrderedLocalDefinitions(@case.Value, order, outerBindings).Concat(OrderedLocalDefinitions(@case.Clauses, order, outerBindings)),
            Expr.If @if => OrderedLocalDefinitions(@if.Clauses, order, outerBindings),
            _ => []
        };
    }

    private IReadOnlyList<Clause>? ReadLiteralTupleComprehensionCase(IReadOnlyList<Clause> clauses)
    {
        if (clauses is not
            [
            {
                Guard: null,
                Patterns: var parameters,
                Body: Expr.Case { Value: Expr.Tuple { Items: var selectors }, Clauses: var arms }
            }
            ]
            || selectors.Count == 0 || arms.Count < 2)
        {
            return null;
        }

        var incoming = parameters.OfType<Pattern.Variable>()
            .Select(parameter => parameter.Name)
            .Where(name => name != VariableScopeNames.Wildcard)
            .ToHashSet(StringComparer.Ordinal);
        if (!selectors.All(IsIncomingSelector))
        {
            return null;
        }

        var keys = new HashSet<Term>();
        foreach (var arm in arms)
        {
            if (arm is not { Guard: null, Patterns: [Pattern.Tuple { Items: var fields }] }
                || fields.Count != selectors.Count
                || !fields.All(field => field is Pattern.Literal { Value: Atom })
                || !keys.Add(Term.Tuple(fields.Cast<Pattern.Literal>().Select(field => field.Value).ToArray())))
            {
                return null;
            }
        }

        return arms;

        bool IsIncomingSelector(Expr expression) => expression is Expr.Variable input && incoming.Contains(input.Name)
            || expression is Expr.Call { DynamicRemote: false, Module: not (null or ErlangModuleNames.Module) } call
                && call.Module != sourceModule?.Name
                && call.Arguments.Any(argument => argument is Expr.Variable variable && incoming.Contains(variable.Name))
                && call.Arguments.All(argument => argument is Expr.Literal
                    || argument is Expr.Variable variable && incoming.Contains(variable.Name));
    }

    private IEnumerable<Expr> OrderLiteralTupleComprehensionCase(
        IReadOnlyList<Clause> arms,
        ListComprehensionOrder order
    )
    {
        int width = ((Pattern.Tuple)arms[0].Patterns[0]).Items.Count;

        return Partition(arms, 0).SelectMany(arm => OrderedLocalDefinitions(arm.Body, order));

        IEnumerable<Clause> Partition(IEnumerable<Clause> clauses, int field)
        {
            if (field == width)
            {
                return clauses;
            }

            // Constructors are visited in first-source order for SSA names.
            // The definition walk visits the value groups from right to left.
            var groups = clauses.GroupBy(clause => ((Pattern.Literal)((Pattern.Tuple)clause.Patterns[0]).Items[field]).Value);
            var ordered = order == ListComprehensionOrder.Definition ? groups.Reverse() : groups;

            return ordered.SelectMany(group => Partition(group, field + 1));
        }
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
