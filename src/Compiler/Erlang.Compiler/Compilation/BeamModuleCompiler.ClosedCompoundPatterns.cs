namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private FunctionDefinition FoldClosedEntryListPattern(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || function.Clauses is not
                [
                { Patterns: [Pattern.List { Items.Count: > 0 } pattern] } first,
                { Patterns: [global::Erlang.Pattern.Variable or global::Erlang.Pattern.Any], Guard: null } fallback
                ]
                || ReadClosedPattern(pattern) is not Cons value)
            {
                return function;
            }

            // A closed cons has no binding to retain. The next clause accepts
            // every other value of the same original argument.
            return function with
            {
                Clauses = [first with { Patterns = [new Pattern.Literal(value)] }, fallback]
            };
        }

        private Expr.Case FoldClosedCasePatterns(Expr.Case decision)
        {
            if (owner.ModuleOptimizationDisabled)
            {
                return decision;
            }

            Clause[]? rewritten = null;
            for (int first = 0; first < decision.Clauses.Count;)
            {
                if (!HasCaseConstructor(decision.Clauses[first]))
                {
                    first++;
                    continue;
                }

                // Like beam_core_to_ssa, partition consecutive constructor rows before selecting by type.
                int end = first + 1;
                while (end < decision.Clauses.Count && HasCaseConstructor(decision.Clauses[end]))
                {
                    end++;
                }

                Pattern[] patterns = decision.Clauses.Skip(first).Take(end - first)
                    .Select(clause => clause.Patterns[0]).ToArray();
                for (int index = 0; index < patterns.Length; index++)
                {
                    Pattern pattern = patterns[index];
                    Pattern folded = FoldCaseConstructor(
                        pattern,
                        patterns.Where((_, peer) => peer != index).Select(EntryShape).ToArray(),
                        foldTuple: decision.Value is not Expr.Tuple
                    );
                    if (!ReferenceEquals(pattern, folded))
                    {
                        rewritten ??= decision.Clauses.ToArray();
                        rewritten[first + index] = rewritten[first + index] with { Patterns = [folded] };
                    }
                }

                first = end;
            }

            return rewritten is null ? decision : decision with { Clauses = rewritten };
        }

        private static bool HasCaseConstructor(Clause clause) =>
            clause.Patterns is [var pattern]
                && EntryShape(pattern) is not (Erlang.Pattern.Variable or Erlang.Pattern.Any or AliasPattern);

        private static Pattern FoldCaseConstructor(Pattern pattern, Pattern[] peers, bool foldTuple = true)
        {
            if (pattern is Pattern.Tuple tuple)
            {
                // Core case optimization decomposes a known tuple input before SSA selects its patterns.
                if (foldTuple && !peers.Any(IsTupleConstructor) && ReadClosedPattern(tuple) is { } literal)
                {
                    return new Pattern.Literal(literal);
                }

                Pattern[]? members = null;
                if (peers.Any(peer => peer is Pattern.Literal { Value: TupleTerm }))
                {
                    return tuple;
                }

                var overlapping = peers.OfType<Pattern.Tuple>().Where(peer => peer.Items.Count == tuple.Items.Count).ToArray();
                for (int index = 0; index < tuple.Items.Count; index++)
                {
                    Pattern member = tuple.Items[index];
                    var column = overlapping.Select(peer => EntryShape(peer.Items[index])).ToArray();
                    if (!column.Any(peer => peer is Erlang.Pattern.Variable or Erlang.Pattern.Any or AliasPattern))
                    {
                        Pattern folded = FoldCaseConstructor(member, column);
                        if (!ReferenceEquals(member, folded))
                        {
                            members ??= tuple.Items.ToArray();
                            members[index] = folded;
                        }
                    }

                    if (member is Pattern.Literal selected)
                    {
                        overlapping = overlapping.Where(peer =>
                            peer.Items[index] is not Pattern.Literal literalPeer || literalPeer.Value.Equals(selected.Value)
                        ).ToArray();
                    }
                }

                return members is null ? tuple : tuple with { Items = members };
            }

            if (pattern is Pattern.List { Items.Count: > 0 } list && !peers.Any(IsConsConstructor)
                && ReadClosedPattern(list) is { } cons)
            {
                return new Pattern.Literal(cons);
            }

            if (pattern is Pattern.List { Items: [var head, Pattern.Literal { Value: Atom last }], Tail: null } pair
                && HasDisjointConsHeads(head, peers))
            {
                // The first cons splits into a head and tail column. Once the
                // atom and tuple heads separate, the closed tail has no cons peer.
                return pair with { Items = [head], Tail = new Pattern.Literal(Term.List(last)) };
            }

            return pattern;
        }

        private static bool HasDisjointConsHeads(Pattern head, Pattern[] peers)
        {
            var own = EntryShape(head);
            bool hasPeer = false;
            foreach (Pattern peer in peers)
            {
                Pattern? other = peer switch
                {
                    Pattern.List { Items.Count: > 0 } list => EntryShape(list.Items[0]),
                    Pattern.Literal { Value: Cons cons } => new Pattern.Literal(cons.Head),
                    _ => null
                };
                if (other is null)
                {
                    continue;
                }

                hasPeer = true;
                if (!(own is Pattern.Literal { Value: Atom } && IsTupleConstructor(other)
                    || other is Pattern.Literal { Value: Atom } && IsTupleConstructor(own)))
                {
                    return false;
                }
            }

            return hasPeer;
        }

        private static bool IsTupleConstructor(Pattern pattern) => pattern is
            Erlang.Pattern.Tuple or Erlang.Pattern.Literal { Value: TupleTerm };

        private static bool IsConsConstructor(Pattern pattern) => pattern is
            Erlang.Pattern.List { Items.Count: > 0 } or Erlang.Pattern.Literal { Value: Cons };

        private static Term? ReadClosedPattern(Pattern pattern)
        {
            switch (pattern)
            {
                case Pattern.Literal literal:
                    return literal.Value;
                case Pattern.Tuple tuple when ReadClosedPatterns(tuple.Items) is { } members:
                    return Term.Tuple(members);
                case Pattern.List list when ReadClosedPatterns(list.Items) is { } items:
                    Term? tail = list.Tail is null ? Nil.Value : ReadClosedPattern(list.Tail);

                    return tail is null ? null : Cons.From(items, tail);
                default:
                    return null;
            }
        }

        private static Term[]? ReadClosedPatterns(IReadOnlyList<Pattern> patterns)
        {
            var terms = new Term[patterns.Count];
            for (int index = 0; index < patterns.Count; index++)
            {
                if (ReadClosedPattern(patterns[index]) is not { } term)
                {
                    return null;
                }

                terms[index] = term;
            }

            return terms;
        }
    }
}
