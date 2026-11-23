namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private static Clause[]? OrderMixedTupleAtomClauses(IReadOnlyList<Clause> clauses)
        {
            if (clauses.Count < 3)
            {
                return null;
            }

            var tupleClauses = new List<Clause>();
            var arities = new HashSet<int>();
            Clause? atom = null;
            Clause? fallback = null;
            for (int index = 0; index < clauses.Count; index++)
            {
                Clause clause = clauses[index];
                if (clause is not { Guard: null, Patterns: [var pattern] })
                {
                    return null;
                }

                switch (pattern)
                {
                    case Erlang.Pattern.Tuple tuple when tuple.Items.All(item => item is
                        Erlang.Pattern.Variable or Erlang.Pattern.Any or Erlang.Pattern.Literal or Erlang.Pattern.List { Items.Count: 0, Tail: null }):
                        tupleClauses.Add(clause);
                        arities.Add(tuple.Items.Count);

                        break;
                    case Erlang.Pattern.Literal { Value: Atom } when atom is null:
                        atom = clause;

                        break;
                    case Erlang.Pattern.Variable { Name: VariableScopeNames.Wildcard } when index == clauses.Count - 1:
                        fallback = clause;

                        break;
                    default:
                        return null;
                }
            }

            if (arities.Count < 2 || atom is null)
            {
                return null;
            }

            // Constructor types are disjoint. The shared arity planner keeps
            // overlapping clauses in order inside each tuple arity.
            return fallback is null ? [.. tupleClauses, atom] : [.. tupleClauses, atom, fallback];
        }
    }
}
