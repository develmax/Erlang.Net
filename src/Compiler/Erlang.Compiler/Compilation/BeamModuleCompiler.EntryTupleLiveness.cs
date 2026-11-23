namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private FunctionDefinition PruneUnusedEntryTupleElements(FunctionDefinition function)
    {
        if (ModuleOptimizationDisabled || function.Clauses.Count < 2)
        {
            return function;
        }

        Clause[]? rewritten = null;
        for (int index = 0; index < function.Clauses.Count; index++)
        {
            Clause original = function.Clauses[index];
            Clause pruned = PruneClause(original);
            if (ReferenceEquals(pruned, original))
            {
                continue;
            }

            rewritten ??= function.Clauses.ToArray();
            rewritten[index] = pruned;
        }

        return rewritten is null ? function : function with { Clauses = rewritten };

        static Clause PruneClause(Clause clause)
        {
            if (!clause.Patterns.Any(pattern => pattern is Pattern.Tuple or AliasPattern { Left: Pattern.Tuple } or AliasPattern { Right: Pattern.Tuple })
                || !clause.Patterns.Any(pattern => pattern is MapPattern || MapOwner(pattern) is not null))
            {
                return clause;
            }

            // Core's pat_mark_unused only omits tuple projections whose results
            // are absent from the body and from the remaining pattern tests.
            var unused = clause.Patterns.SelectMany(Semantics.Variables)
                .GroupBy(name => name, StringComparer.Ordinal)
                .Where(group => group.Count() == 1)
                .Select(group => group.Key)
                .ToHashSet(StringComparer.Ordinal);
            try
            {
                unused.ExceptWith(ClosureCaptures.ReadEntryReferences(clause, unused));
            }
            catch (NotSupportedException)
            {
                return clause;
            }

            return unused.Count == 0
                ? clause
                : clause with { Patterns = clause.Patterns.Select(OmitFromHead).ToArray() };

            Pattern OmitFromHead(Pattern pattern) => pattern switch
            {
                Pattern.Tuple tuple => tuple with { Items = tuple.Items.Select(OmitFromElement).ToArray() },
                AliasPattern alias => alias with { Left = OmitFromHead(alias.Left), Right = OmitFromHead(alias.Right) },
                _ => pattern
            };

            Pattern OmitFromElement(Pattern pattern) =>
                pattern is Pattern.Variable variable && unused.Contains(variable.Name)
                    ? new Pattern.Variable(VariableScopeNames.Wildcard)
                    : OmitFromHead(pattern);
        }
    }
}
