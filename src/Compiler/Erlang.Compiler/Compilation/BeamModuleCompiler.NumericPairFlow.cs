namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    [Flags]
    private enum PairFacts
    {
        Unknown = 0,
        Integer = 1,
        Natural = Integer | 2,
        Length = 4,
        ProperList = 8,
        Positive = Natural | 16,
        PositiveLength = Length | 32,
        NonemptyList = 64,
        BitValue = Natural | 128,
        BitList = ProperList | 256,
        Numeric = 512,
        NumericLength = 1024,
        NonnegativeNumber = 2048,
        NonpositiveNumber = 4096,
        NonnegativeIfNumeric = 8192,
        Atom = 16384,
        List = 32768,
        NoReturn = Positive | PositiveLength | ProperList | NonemptyList
    }

    private NumericPairFlow? numericPairFlow;

    private sealed class NumericPairFlow(PairFacts top)
    {
        internal sealed class Entry(PairFacts[] arguments, PairFacts result)
        {
            internal PairFacts[] Arguments { get; set; } = arguments;
            internal PairFacts Result { get; set; } = result;
        }

        internal Dictionary<(string Name, int Arity), Entry> Entries { get; } = [];
        internal bool Changed { get; set; }

        internal void Observe((string Name, int Arity) signature, PairFacts[] arguments)
        {
            if (!Entries.TryGetValue(signature, out var entry))
            {
                Entries.Add(signature, new Entry(arguments, top));
                Changed = true;

                return;
            }

            var joined = arguments.Zip(entry.Arguments, (incoming, prior) => incoming & prior).ToArray();
            if (!joined.SequenceEqual(entry.Arguments))
            {
                entry.Arguments = joined;
                Changed = true;
            }
        }
    }

    private sealed partial class GeneralEmitter
    {
        private NumericPairFlow? activeNumericPairFlow;
        private bool collectNumericPairCalls;

        private NumericPairFlow GetNumericPairFlow()
        {
            if (owner.numericPairFlow is { } cached)
            {
                return cached;
            }

            inferNumericPairs = true;
            observePairCalls = true;
            collectNumericPairCalls = true;
            var flow = new NumericPairFlow(PairAnalysisTop);
            activeNumericPairFlow = flow;
            try
            {
                var roots = owner.exportedFunctions.Concat(localFunctions.Keys.Where(owner.IsDeclaredNative))
                    .Concat(localFunctions.Values.SelectMany(function => LocalFunctionEntries.ClauseExpressions(function.Clauses))
                        .SelectMany(LocalFunctionEntries.Walk).OfType<Expr.FunctionReference>()
                        .Select(reference => (reference.Name, reference.Arity)));
                if (owner.OnLoadFunction is { } onLoad)
                {
                    roots = roots.Append((onLoad, 0));
                }

                foreach (var signature in roots.Where(localFunctions.ContainsKey))
                {
                    flow.Observe(signature, new PairFacts[signature.Arity]);
                }

                var unusedContexts = new Dictionary<(string Function, string Arguments), PairFacts>();
                do
                {
                    flow.Changed = false;
                    foreach (var (signature, entry) in flow.Entries.ToArray())
                    {
                        PairFacts result = entry.Result & ReadPairBranches(
                            localFunctions[signature].Clauses,
                            entry.Arguments,
                            new(StringComparer.Ordinal),
                            unusedContexts
                        );
                        flow.Changed |= result != entry.Result;
                        entry.Result = result;
                    }
                }
                while (flow.Changed);
            }
            finally
            {
                activeNumericPairFlow = null;
                collectNumericPairCalls = false;
                observePairCalls = false;
                inferNumericPairs = false;
            }

            return owner.numericPairFlow = flow;
        }

        private PairFacts ReadJoinedNumericPairFacts(Expr expression)
        {
            activeNumericPairFlow = GetNumericPairFlow();
            inferNumericPairs = true;
            try
            {
                return ReadPairFacts(expression, new(StringComparer.Ordinal), []);
            }
            finally
            {
                inferNumericPairs = false;
                activeNumericPairFlow = null;
            }
        }

        private PairFacts ReadJoinedNumericArgument(FunctionDefinition function, int index)
        {
            var flow = GetNumericPairFlow();

            return flow.Entries.TryGetValue((function.Name, function.Arity), out var entry)
                ? entry.Arguments[index] : PairFacts.Unknown;
        }

        internal int ReadAtomEntryType(FunctionDefinition function, int index) =>
            owner.exportedFunctions.Contains((function.Name, function.Arity))
                || owner.IsDeclaredNative((function.Name, function.Arity))
                || (ReadJoinedNumericArgument(function, index) & PairFacts.Atom) == 0
                ? 0 : AtomTypeBits;

        private int ReadSavedComparisonEntryType(FunctionDefinition function, int index)
        {
            var signature = (function.Name, function.Arity);
            if (owner.exportedFunctions.Contains(signature) || owner.IsDeclaredNative(signature))
            {
                return 0;
            }

            PairFacts facts = ReadJoinedNumericArgument(function, index);

            return (facts & (PairFacts.Atom | PairFacts.Integer | PairFacts.List)) switch
            {
                PairFacts.Atom => AtomTypeBits,
                PairFacts.Integer => IntegerTypeBits,
                PairFacts.List => ProperListTypeBits,
                _ => 0
            };
        }

        private void RefineEntryTypeGuard(Expr guard, Dictionary<string, PairFacts> bindings)
        {
            if (guard is Expr.Call
                {
                    DynamicRemote: false, Arguments: [Expr.Variable variable]
                } call && owner.ResolvesErlangBif(call))
            {
                var facts = call.Function switch
                {
                    AutoImportNames.IsAtom => PairFacts.Atom,
                    "is_integer" => PairFacts.Integer | PairFacts.Numeric,
                    "is_list" => PairFacts.List,
                    _ => PairFacts.Unknown
                };
                if (facts != PairFacts.Unknown)
                {
                    bindings[variable.Name] = bindings.GetValueOrDefault(variable.Name) | facts;
                }
            }
            else if (guard is Expr.Binary { Operator: ErlangOperators.And or ErlangOperators.AndAlso } conjunction)
            {
                RefineEntryTypeGuard(conjunction.Left, bindings);
                RefineEntryTypeGuard(conjunction.Right, bindings);
            }
        }

        private static void RefineNumericComparison(
            Expr? guard, Dictionary<string, PairFacts> bindings, bool succeeded
        )
        {
            if (guard is not Expr.Binary
                {
                    Left: Expr.Variable variable,
                    Right: Expr.Literal { Value: Integer limit }
                }
                || !bindings.TryGetValue(variable.Name, out PairFacts facts))
            {
                return;
            }

            // In term order all nonnumeric terms are greater than every number.
            bool numeric = (facts & PairFacts.Numeric) != 0;
            PairFacts constraint = (guard, succeeded) switch
            {
                (Expr.Binary { Operator: ErlangOperators.Less or ErlangOperators.LessOrEqual }, true)
                    or (Expr.Binary { Operator: ErlangOperators.Greater or ErlangOperators.GreaterOrEqual }, false)
                    => limit.Value <= 0 ? PairFacts.Numeric | PairFacts.NonpositiveNumber : PairFacts.Numeric,
                (Expr.Binary { Operator: ErlangOperators.Greater or ErlangOperators.GreaterOrEqual }, true)
                    or (Expr.Binary { Operator: ErlangOperators.Less or ErlangOperators.LessOrEqual }, false)
                    when limit.Value >= 0 => PairFacts.NonnegativeIfNumeric
                        | (numeric ? PairFacts.NonnegativeNumber : PairFacts.Unknown),
                _ => PairFacts.Unknown
            };
            bindings[variable.Name] = facts | constraint;
        }

        private static IReadOnlyList<PairFacts> RefineFailedNumericClause(
            Clause clause, Dictionary<string, PairFacts> bindings, IReadOnlyList<PairFacts> arguments
        )
        {
            if (clause.Guard is not Expr.Binary { Left: Expr.Variable tested }
                || clause.Patterns.Any(pattern => pattern is not Pattern.Variable { }))
            {
                return arguments;
            }

            var variables = clause.Patterns.Cast<Pattern.Variable>().Select(variable => variable.Name).ToArray();
            int index = Array.IndexOf(variables, tested.Name);
            if (index < 0 || variables.Where(name => name != VariableScopeNames.Wildcard)
                .GroupBy(name => name).Any(group => group.Count() != 1 || bindings.ContainsKey(group.Key)))
            {
                return arguments;
            }

            var scope = new Dictionary<string, PairFacts>(StringComparer.Ordinal) { [tested.Name] = arguments[index] };
            RefineNumericComparison(clause.Guard, scope, succeeded: false);
            var refined = arguments.ToArray();
            refined[index] = scope[tested.Name];

            return refined;
        }
    }
}
