namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private const int MaximumFiniteAtomSetSize = 5; // OTP beam_types:join/2 atom set limit.
    private AtomCallSites? atomCallSites;

    private sealed record FiniteAtomLiteralDispatch((Term Key, Term Result)[] Branches, int LabelCount);

    private void EmitLiteralReturn(Term result)
    {
        EmitConstant(result, 0);
        instructions.WriteByte(BeamEmissionFormat.Return);
    }

    private sealed class AtomCallSites
    {
        internal Dictionary<(string Name, int Arity), List<Site>> Direct { get; } = [];
        internal HashSet<(string Name, int Arity)> Indirect { get; } = [];

        internal sealed record Site(
            FunctionDefinition Caller,
            Clause Clause,
            Expr.Call Call,
            bool InNestedScope
        );
    }

    private sealed partial class GeneralEmitter
    {
        internal FiniteAtomLiteralDispatch? ReadFiniteAtomLiteralDispatch(FunctionDefinition function)
        {
            var clauses = function.Clauses;
            if (!clauses.All(clause => clause is { Guard: null, Patterns: [Pattern.Literal { Value: Atom }] })
                || !HasLiteralDispatchKeys(clauses)
                || ReadFiniteAtomArguments(function, 0) is not { } values)
            {
                return null;
            }

            var branches = clauses.Select(clause =>
                    (Key: ((Pattern.Literal)clause.Patterns[0]).Value, Result: DispatchResult(clause)))
                .Where(branch => values.Contains((Atom)branch.Key))
                .OrderByDescending(branch => branch.Key, Comparer<Term>.Create(
                    (left, right) => TermOrder.Compare(left, right, exact: true)
                )).ToArray();
            if (branches.Length != values.Count)
            {
                return null;
            }

            int results = branches.Select(branch => branch.Result).Distinct().Count();
            int labels = results == 1 ? 0 : branches.Length == 2 ? 1 : results;

            return new FiniteAtomLiteralDispatch(branches, labels);
        }

        internal IReadOnlySet<Atom>? ReadFiniteAtomArguments(FunctionDefinition function, int index)
        {
            if (ReadAtomEntryType(function, index) == 0)
            {
                return null;
            }

            var sites = owner.atomCallSites ??= IndexAtomCallSites();
            var visited = new HashSet<(string Name, int Arity, int Index)>();
            var pending = new Stack<(string Name, int Arity, int Index)>();
            var values = new HashSet<Atom>();
            pending.Push((function.Name, function.Arity, index));
            while (pending.TryPop(out var argument))
            {
                if (!visited.Add(argument))
                {
                    continue;
                }

                var signature = (argument.Name, argument.Arity);
                if (owner.exportedFunctions.Contains(signature) || owner.IsDeclaredNative(signature)
                    || signature == (owner.OnLoadFunction, 0) || sites.Indirect.Contains(signature)
                    || !sites.Direct.TryGetValue(signature, out var calls))
                {
                    return null;
                }

                foreach (var site in calls)
                {
                    Expr input = site.Call.Arguments[argument.Index];
                    var local = new HashSet<Atom>();
                    if (CollectReturnedAtoms(input, local))
                    {
                        values.UnionWith(local);
                        continue;
                    }

                    if (site.InNestedScope || input is not Expr.Variable variable)
                    {
                        return null;
                    }

                    int source = FindAtomParameter(site.Clause.Patterns, variable.Name);
                    if (source >= 0)
                    {
                        pending.Push((site.Caller.Name, site.Caller.Arity, source));
                    }
                    else if (FindPriorAtomBinding(site, variable.Name) is { } binding && CollectReturnedAtoms(binding, values))
                    {
                        continue;
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            return values.Count is > 0 and <= MaximumFiniteAtomSetSize ? values : null;
        }

        private static Expr? FindPriorAtomBinding(AtomCallSites.Site site, string variable)
        {
            if (site.Clause.Body is not Expr.Sequence sequence)
            {
                return null;
            }

            Expr? binding = null;
            foreach (Expr expression in sequence.Items)
            {
                if (LocalFunctionEntries.Walk(expression).Any(item => ReferenceEquals(item, site.Call)))
                {
                    return binding;
                }

                if (expression is Expr.Match { Pattern: Pattern.Variable name, Value: var value } && name.Name == variable)
                {
                    binding = value;
                }
            }

            return null;
        }

        private bool CollectReturnedAtoms(
            Expr expression,
            HashSet<Atom> values,
            HashSet<(string Name, int Arity)>? active = null
        )
        {
            switch (expression)
            {
                case Expr.Literal { Value: Atom atom }:
                    values.Add(atom);

                    return true;
                case Expr.Case decision:
                    return decision.Clauses.All(clause => CollectReturnedAtoms(clause.Body, values, active));
                case Expr.If decision:
                    return decision.Clauses.All(clause => CollectReturnedAtoms(clause.Body, values, active));
                case Expr.Sequence { Items.Count: > 0 } sequence:
                    return CollectReturnedAtoms(sequence.Items[^1], values, active);
                case Expr.Block block:
                    return CollectReturnedAtoms(block.Body, values, active);
                case Expr.Call { Module: null, DynamicRemote: false } call:
                    var signature = (call.Function, call.Arguments.Count);
                    active ??= [];
                    if (!localFunctions.TryGetValue(signature, out var target) || owner.IsDeclaredNative(signature)
                        || !active.Add(signature))
                    {
                        return false;
                    }

                    bool known = target.Clauses.All(clause => CollectReturnedAtoms(clause.Body, values, active));
                    active.Remove(signature);

                    return known;
                default:
                    return false;
            }
        }

        private static int FindAtomParameter(IReadOnlyList<Pattern> patterns, string variable)
        {
            for (int index = 0; index < patterns.Count; index++)
            {
                if (patterns[index] is Pattern.Variable parameter && parameter.Name == variable)
                {
                    return index;
                }
            }

            return -1;
        }

        private AtomCallSites IndexAtomCallSites()
        {
            var sites = new AtomCallSites();
            foreach (FunctionDefinition caller in localFunctions.Values)
            {
                foreach (Clause clause in caller.Clauses)
                {
                    foreach (Expr expression in LocalFunctionEntries.ClauseExpressions([clause]))
                    {
                        Collect(
                            expression,
                            caller,
                            clause,
                            inNestedScope: false
                        );
                    }
                }
            }

            return sites;

            void Collect(
                Expr expression,
                FunctionDefinition caller,
                Clause clause,
                bool inNestedScope
            )
            {
                if (expression is Expr.FunctionReference reference)
                {
                    sites.Indirect.Add((reference.Name, reference.Arity));
                }
                else if (expression is Expr.Call { Module: null, DynamicRemote: false } call)
                {
                    var signature = (call.Function, call.Arguments.Count);
                    if (localFunctions.ContainsKey(signature))
                    {
                        if (!sites.Direct.TryGetValue(signature, out var calls))
                        {
                            calls = [];
                            sites.Direct.Add(signature, calls);
                        }

                        calls.Add(new AtomCallSites.Site(
                            caller,
                            clause,
                            call,
                            inNestedScope
                        ));
                    }
                }

                bool nested = inNestedScope || expression is Expr.Fun or Expr.ListComprehension
                    or Expr.BinaryComprehension or Expr.MapComprehension;
                foreach (Expr child in LocalFunctionEntries.Children(expression))
                {
                    Collect(
                        child,
                        caller,
                        clause,
                        nested
                    );
                }
            }
        }
    }
}
