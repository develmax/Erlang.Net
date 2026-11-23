namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private IReadOnlyList<Clause> AliasNestedOrderedConsHeads(IReadOnlyList<Clause> clauses)
        {
            if (owner.ModuleOptimizationDisabled)
            {
                return clauses;
            }

            Clause[]? rewritten = null;
            for (int index = 0; index < clauses.Count; index++)
            {
                Clause source = clauses[index];
                Clause aliased = AliasNestedOrderedConsHead(source);
                if (!ReferenceEquals(source, aliased))
                {
                    rewritten ??= clauses.ToArray();
                    rewritten[index] = aliased;
                }
            }

            return rewritten ?? clauses;
        }

        private Clause AliasNestedOrderedConsHead(Clause clause)
        {
            if (clause is not
                {
                    Guard: null,
                    Body: Expr.List { Items.Count: > 1, Tail: Expr.Call tail } list
                }
                || !clause.Patterns.Any(pattern => pattern is Erlang.Pattern.List)
                || !TryResolveOrdinaryCall(tail, out string? module) || module == ErlangModuleNames.Module)
            {
                return clause;
            }

            var bindings = clause.Patterns.SelectMany(Semantics.Variables).ToLookup(name => name, StringComparer.Ordinal);
            if (tail.Arguments.OfType<Expr.Variable>().Any(argument => !bindings.Contains(argument.Name))
                || ReadOrderedConsConstruction(list, bindings.Select(group => group.Key).ToArray(), null) is null)
            {
                return clause;
            }

            Erlang.Pattern[]? patterns = null;
            Expr[]? heads = null;
            int ordinal = 0;
            for (int parameter = 0; parameter < clause.Patterns.Count; parameter++)
            {
                if (clause.Patterns[parameter] is not Erlang.Pattern.List input)
                {
                    continue;
                }

                Erlang.Pattern[]? cells = null;
                for (int cell = 0; cell < input.Items.Count; cell++)
                {
                    if (input.Items[cell] is not Erlang.Pattern.Tuple token || !HasStableConstructorKey(token))
                    {
                        continue;
                    }

                    var matching = Enumerable.Range(0, list.Items.Count)
                        .Where(index => list.Items[index] is Expr.Tuple rebuilt && ReturnsMatchedValue(token, rebuilt))
                        .ToArray();
                    if (matching.Length == 0)
                    {
                        continue;
                    }

                    // sys_core_alias creates a binding on the matching pattern,
                    // then replaces each reconstruction of that exact key.
                    var alias = FormattableString.Invariant($"\0ordered-cons-alias-{ordinal++}");
                    cells ??= input.Items.ToArray();
                    cells[cell] = new AliasPattern(new Erlang.Pattern.Variable(alias), token);
                    heads ??= list.Items.ToArray();
                    foreach (int index in matching)
                    {
                        heads[index] = new Expr.Variable(alias);
                    }
                }

                if (cells is not null)
                {
                    patterns ??= clause.Patterns.ToArray();
                    patterns[parameter] = input with { Items = cells };
                }
            }

            return patterns is null ? clause : clause with
            {
                Patterns = patterns,
                Body = list with { Items = heads! }
            };

            bool HasStableConstructorKey(Erlang.Pattern.Tuple tuple) =>
                tuple.Items.Any(item => item is Erlang.Pattern.Variable)
                && tuple.Items.All(item => item is Erlang.Pattern.Literal { Value: Atom or Integer or Nil }
                    || item is Erlang.Pattern.Variable { Name: not VariableScopeNames.Wildcard } variable
                        && bindings[variable.Name].Count() == 1);
        }
    }
}
