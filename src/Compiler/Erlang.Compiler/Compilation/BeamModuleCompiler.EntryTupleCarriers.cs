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
                if (function.Clauses.Count > 1 && original is Pattern.List root
                    && ReuseTerminalEntryCons(
                        clause,
                        root,
                        clauseIndex,
                        index
                    ) is { } retained)
                {
                    patterns ??= clause.Patterns.ToArray();
                    patterns[index] = retained.Pattern;
                    body = retained.Body;
                    continue;
                }

                if (original is Pattern.Tuple container
                    && ReuseNestedEntryCons(
                        clause,
                        container,
                        body,
                        clauseIndex,
                        index
                    ) is { } nested)
                {
                    patterns ??= clause.Patterns.ToArray();
                    patterns[index] = nested.Pattern;
                    body = nested.Body;
                    continue;
                }

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
                Expr replacement = ReuseEntryCarrierInBody(body, tuple, alias);
                if (list is { Tail: Pattern.Variable { Name: not VariableScopeNames.Wildcard } }
                    && tuple.Items.Count == 2 && ReferenceEquals(body, replacement)
                    && ReuseEntryPairConsInBooleanCase(
                        function,
                        body,
                        tuple,
                        alias
                    ) is { } reused)
                {
                    replacement = reused;
                    HashSet<string> used;
                    try
                    {
                        used = ClosureCaptures.ReadEntryReferences(
                            clause with { Body = replacement },
                            tuple.Items.Cast<Pattern.Variable>().Select(variable => variable.Name)
                        );
                    }
                    catch (NotSupportedException)
                    {
                        continue;
                    }

                    tuple = tuple with
                    {
                        Items = tuple.Items.Cast<Pattern.Variable>().Select<Pattern.Variable, Pattern>(variable =>
                            used.Contains(variable.Name) ? variable : new Pattern.Variable(VariableScopeNames.Wildcard)
                        ).ToArray()
                    };
                }

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

    private static Expr? ReuseEntryPairConsInBooleanCase(
        FunctionDefinition function,
        Expr expression,
        Pattern.Tuple checkedPair,
        Expr.Variable carrier
    )
    {
        if (expression is not Expr.Case
            {
                Value: Expr.Call { Module: not null, DynamicRemote: false } selector,
                Clauses:
                [
                { Guard: null, Patterns: [Pattern.Literal { Value: Atom first }], Body: var onFirst },
                { Guard: null, Patterns: [Pattern.Literal { Value: Atom second }], Body: var onSecond }
                ]
            } decision
            || first.Name is not (ErlangBooleanAtoms.True or ErlangBooleanAtoms.False)
            || second.Name is not (ErlangBooleanAtoms.True or ErlangBooleanAtoms.False)
            || first.Name == second.Name || !HasBindingFreeSelector(selector)
            || onFirst is not Expr.Call firstCall || !IsRecursion(firstCall)
            || onSecond is not Expr.Call secondCall || !IsRecursion(secondCall))
        {
            return null;
        }

        // The two literal arms introduce no bindings. A direct recursive
        // cons head that reconstructs this checked pair can reuse its source.
        Expr.Call firstRewritten = Rewrite(firstCall);
        Expr.Call secondRewritten = Rewrite(secondCall);

        return ReferenceEquals(firstCall, firstRewritten) && ReferenceEquals(secondCall, secondRewritten)
            ? null
            : decision with
            {
                Clauses =
                [
                    decision.Clauses[0] with { Body = firstRewritten },
                    decision.Clauses[1] with { Body = secondRewritten }
                ]
            };

        bool IsRecursion(Expr.Call arm) => arm is { Module: null, DynamicRemote: false }
            && arm.Function == function.Name && arm.Arguments.Count == function.Arity;

        Expr.Call Rewrite(Expr.Call call)
        {
            Expr[]? arguments = null;
            for (int index = 0; index < call.Arguments.Count; index++)
            {
                if (call.Arguments[index] is Expr.List { Items: [Expr.Tuple rebuilt], Tail: Expr.Variable } cons
                    && ReconstructsMatchedValue(checkedPair, rebuilt))
                {
                    arguments ??= call.Arguments.ToArray();
                    arguments[index] = cons with { Items = [carrier] };
                }
            }

            return arguments is null ? call : call with { Arguments = arguments };
        }
    }

    private static (Pattern Pattern, Expr Body)? ReuseTerminalEntryCons(
        Clause clause,
        Pattern.List cons,
        int clauseIndex,
        int input
    )
    {
        if (cons is not
            {
                Items: [Pattern.Variable { Name: not VariableScopeNames.Wildcard } head],
                Tail: Pattern.Variable { Name: not VariableScopeNames.Wildcard } tail
            }
            || clause.Patterns.Where((_, index) => index != input).Any(pattern => pattern is not Pattern.Variable)
            || clause.Body is not Expr.Call { Module: null, DynamicRemote: false } call)
        {
            return null;
        }

        int matches = 0;
        foreach (Expr argument in call.Arguments)
        {
            if (ReconstructsMatchedValue(cons, argument))
            {
                matches++;
            }
            else if (argument is not (Expr.Variable or Expr.Literal))
            {
                return null;
            }
        }

        if (matches != 1)
        {
            return null;
        }

        string name = FormattableString.Invariant($"$entry_root_cons_carrier_{clauseIndex}_{input}");
        Expr replacement = ReuseEntryCarrierInBody(call, cons, new Expr.Variable(name));
        if (ReferenceEquals(call, replacement))
        {
            return null;
        }

        HashSet<string> used;
        try
        {
            used = ClosureCaptures.ReadEntryReferences(clause with { Body = replacement }, [head.Name, tail.Name]);
        }
        catch (NotSupportedException)
        {
            return null;
        }

        if (used.Contains(tail.Name))
        {
            return null;
        }

        // Preserve the cons test even when its components no longer need projection.
        var wildcard = new Pattern.Variable(VariableScopeNames.Wildcard);
        var alias = new AliasPattern(
            new Pattern.Variable(name),
            cons with { Items = [used.Contains(head.Name) ? head : wildcard], Tail = wildcard }
        );

        return (alias, replacement);
    }

    private static (Pattern.Tuple Pattern, Expr Body)? ReuseNestedEntryCons(
        Clause clause,
        Pattern.Tuple container,
        Expr body,
        int clauseIndex,
        int input
    )
    {
        Pattern[]? fields = null;
        for (int field = 0; field < container.Items.Count; field++)
        {
            if (container.Items[field] is not Pattern.List
                {
                    Items: [Pattern.Variable { Name: not VariableScopeNames.Wildcard }],
                    Tail: Pattern.Variable { Name: not VariableScopeNames.Wildcard } tail
                } cons)
            {
                continue;
            }

            string name = FormattableString.Invariant($"$entry_cons_carrier_{clauseIndex}_{input}_{field}");
            var alias = new Expr.Variable(name);
            Expr replacement = ReuseEntryCarrierInBody(body, cons, alias);
            if (ReferenceEquals(body, replacement))
            {
                continue;
            }

            try
            {
                if (ClosureCaptures.ReadEntryReferences(clause with { Body = replacement }, [tail.Name]).Contains(tail.Name))
                {
                    continue;
                }
            }
            catch (NotSupportedException)
            {
                continue;
            }

            // All former tail reads were reconstructions of this same cons. Keep the
            // original field and let ordinary pattern lowering project only its needed head.
            fields ??= container.Items.ToArray();
            fields[field] = new AliasPattern(
                new Pattern.Variable(name),
                cons with { Tail = new Pattern.Variable(VariableScopeNames.Wildcard) }
            );
            body = replacement;
        }

        return fields is null ? null : (container with { Items = fields }, body);
    }

    private static Expr ReuseEntryCarrierInBody(
        Expr expression,
        Pattern shape,
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
            case Expr.List list when directUse && ReconstructsMatchedValue(shape, list):
                return alias;
            case Expr.Call { DynamicRemote: false } call:
                return RewriteItems(call.Arguments, directArguments: true) is { } arguments
                    ? call with { Arguments = arguments }
                    : call;
            case Expr.Match match:
                Expr matchValue = ReuseEntryCarrierInBody(match.Value, shape, alias);

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

                    Expr body = ReuseEntryCarrierInBody(
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
                Expr replacement = ReuseEntryCarrierInBody(
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
