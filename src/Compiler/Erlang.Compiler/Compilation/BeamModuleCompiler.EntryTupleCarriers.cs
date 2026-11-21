namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private const int CoreAliasDepthLimit = 100;

    private static FunctionDefinition ReuseEntryTupleCarriers(FunctionDefinition function)
    {
        Clause[]? updated = null;
        for (int clauseIndex = 0; clauseIndex < function.Clauses.Count; clauseIndex++)
        {
            Clause clause = function.Clauses[clauseIndex];
            var names = clause.Patterns.SelectMany(Semantics.Variables)
                .Where(name => name != VariableScopeNames.Wildcard)
                .ToArray();
            if (names.Distinct(StringComparer.Ordinal).Count() != names.Length)
            {
                continue;
            }

            Pattern[]? patterns = null;
            Expr body = clause.Body;
            for (int index = 0; index < clause.Patterns.Count; index++)
            {
                Pattern original = clause.Patterns[index];
                var list = original as Pattern.List;
                var tuple = original as Pattern.Tuple;
                if (list is { Items: [Pattern.Tuple head], Tail: null or Pattern.Variable })
                {
                    tuple = head;
                }

                if (tuple is not { Items.Count: >= 2 and < CoreAliasDepthLimit }
                    || !tuple.Items.All(item => item is Pattern.Variable { Name: not VariableScopeNames.Wildcard }))
                {
                    continue;
                }

                string name = FormattableString.Invariant($"$entry_tuple_carrier_{clauseIndex}_{index}");
                var alias = new Expr.Variable(name);
                Expr replacement = ReuseEntryTupleInBody(body, tuple, alias);
                if (ReferenceEquals(body, replacement))
                {
                    continue;
                }

                patterns ??= clause.Patterns.ToArray();
                var matched = new AliasPattern(new Pattern.Variable(name), tuple);
                patterns[index] = list is null ? matched : list with { Items = [matched] };
                body = replacement;
            }

            if (patterns is not null)
            {
                updated ??= function.Clauses.ToArray();
                updated[clauseIndex] = clause with { Patterns = patterns, Body = body };
            }
        }

        return updated is null ? function : function with { Clauses = updated };
    }

    private static Expr ReuseEntryTupleInBody(
        Expr expression,
        Pattern.Tuple shape,
        Expr.Variable alias,
        bool directUse = false
    )
    {
        // These expression positions keep the entry match's variable identities.
        // In particular, a match can rewrite its RHS without touching its pattern.
        // Do not enter new pattern, generator, closure, receive or protected scopes.
        switch (expression)
        {
            case Expr.Tuple tuple:
                if (directUse && ReconstructsMatchedValue(shape, tuple))
                {
                    return alias;
                }

                return RewriteItems(tuple.Items) is { } fields ? tuple with { Items = fields } : tuple;
            case Expr.Call { DynamicRemote: false } call:
                return RewriteItems(call.Arguments, directArguments: true) is { } arguments
                    ? call with { Arguments = arguments }
                    : call;
            case Expr.Match match:
                Expr matchValue = ReuseEntryTupleInBody(match.Value, shape, alias);

                return ReferenceEquals(matchValue, match.Value) ? match : match with { Value = matchValue };
            case Expr.Sequence sequence:
                return RewriteItems(sequence.Items, directLast: directUse) is { } items
                    ? sequence with { Items = items }
                    : sequence;
            case Expr.If decision:
                Clause[]? arms = null;
                for (int index = 0; index < decision.Clauses.Count; index++)
                {
                    Clause arm = decision.Clauses[index];
                    if (arm.Patterns.Count != 0)
                    {
                        continue;
                    }

                    Expr body = ReuseEntryTupleInBody(
                        arm.Body,
                        shape,
                        alias,
                        directUse: true
                    );
                    if (!ReferenceEquals(body, arm.Body))
                    {
                        arms ??= decision.Clauses.ToArray();
                        arms[index] = arm with { Body = body };
                    }
                }

                return arms is null ? decision : decision with { Clauses = arms };
            default:
                return expression;
        }

        Expr[]? RewriteItems(
            IReadOnlyList<Expr> values,
            bool directArguments = false,
            bool directLast = false
        )
        {
            Expr[]? updated = null;
            for (int index = 0; index < values.Count; index++)
            {
                Expr value = values[index];
                Expr replacement = ReuseEntryTupleInBody(
                    value,
                    shape,
                    alias,
                    directArguments || directLast && index == values.Count - 1
                );
                if (!ReferenceEquals(value, replacement))
                {
                    updated ??= values.ToArray();
                    updated[index] = replacement;
                }
            }

            return updated;
        }
    }
}
