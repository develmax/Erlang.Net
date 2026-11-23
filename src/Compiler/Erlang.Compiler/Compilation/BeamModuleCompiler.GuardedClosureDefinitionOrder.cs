namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private Dictionary<int, int>? ReadGuardedClosureDefinitionOrder(int start, int end)
    {
        if (!SsaTypeInferenceEnabled || ModuleOptimizationDisabled)
        {
            return null;
        }

        Dictionary<int, int>? order = null;
        int index = start;
        while (index < end)
        {
            LiftedClosure closure = closures[index];
            if (closure.LexicalOwner is not { } parent)
            {
                index++;
                continue;
            }

            var positions = new List<int> { index++ };
            while (index < end)
            {
                var next = closures[index];
                if (ReferenceEquals(next.LexicalOwner, parent))
                {
                    positions.Add(index++);
                    continue;
                }

                if (next is { Body: null, General: null })
                {
                    index++;
                    continue;
                }

                break;
            }

            int count = positions.Count;
            if (count == 2 && positions[1] == positions[0] + 1
                && closures[positions[0]].ForwardDefinitionOrder && closures[positions[1]].ForwardDefinitionOrder)
            {
                order ??= new Dictionary<int, int>();
                order.Add(positions[0], positions[1]);
                order.Add(positions[1], positions[0]);

                continue;
            }

            if (count < 3 || !IsClosedTupleGuardFactory(parent, count))
            {
                continue;
            }

            // Core retains the guarded anonymous alternatives in source order.
            // Declared references have lambda slots but add no function definition.
            // The unguarded fallback is visited as a separate branch.
            bool direct = true;
            for (int relative = 0; relative < count; relative++)
            {
                var member = closures[positions[relative]];
                if (member.CaptureCount != 0 || member.SelfName is not null
                    || member.Name != AnonymousClosureName(parent, relative))
                {
                    direct = false;
                    break;
                }
            }

            if (!direct)
            {
                continue;
            }

            order ??= new Dictionary<int, int>();
            for (int relative = 0; relative < count - 1; relative++)
            {
                order.Add(positions[relative], positions[count - 2 - relative]);
            }
        }

        return order;
    }

    private static bool IsClosedTupleGuardFactory(FunctionDefinition parent, int anonymousCount)
    {
        if (parent.Arity != 1 || parent.Clauses[^1] is not
            {
                Guard: null, Patterns: [Pattern.Variable], Body: Expr.Fun { SelfName: null }
            }
            || parent.Clauses.SelectMany(clause => LocalFunctionEntries.Walk(clause.Body))
                .OfType<Expr.Fun>().Count() != anonymousCount)
        {
            return false;
        }

        var keys = new HashSet<Term>();
        foreach (var clause in parent.Clauses.Take(parent.Clauses.Count - 1))
        {
            if (clause is not
                {
                    Patterns: [Pattern.Variable parameter],
                    Guard: Expr.Binary
                    {
                        Operator: ErlangOperators.ExactEqual, Left: Expr.Variable value, Right: Expr.Tuple tuple
                    }
                }
                || value.Name != parameter.Name || !AttributeLiteral.TryNormalizeConstant(tuple, out Term key)
                || !keys.Add(key))
            {
                return false;
            }

            if (clause.Body is Expr.FunctionReference)
            {
                continue;
            }

            if (clause.Body is not Expr.Fun { SelfName: null })
            {
                return false;
            }
        }

        return true;
    }
}
