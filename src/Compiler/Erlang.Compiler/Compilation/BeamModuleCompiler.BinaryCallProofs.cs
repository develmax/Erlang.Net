namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool HasBinaryLocalArgument(
            FunctionDefinition function,
            Expr.Call? binaryRecursion,
            bool trackBinaryPairResults = false,
            (int Arity, int Element)? projectedBinaryTuple = null,
            bool materializedBitstringOnly = false,
            bool trackGuardedBinaryInputs = false
        )
        {
            if (owner.ModuleOptimizationDisabled)
            {
                return false;
            }

            return Prove(function, 0, []);

            bool Prove(FunctionDefinition target, int argument, HashSet<(string Name, int Arity, int Argument)> active)
            {
                var signature = (target.Name, target.Arity);
                var key = (target.Name, target.Arity, argument);
                if (owner.exportedFunctions.Contains(signature) || owner.IsDeclaredNative(signature) || !active.Add(key))
                {
                    return false;
                }

                int calls = 0;
                bool proved = localFunctions.Values.All(caller => caller.Clauses.All(clause =>
                {
                    var binaries = new HashSet<string>(StringComparer.Ordinal);
                    if (materializedBitstringOnly)
                    {
                        foreach (Pattern pattern in clause.Patterns)
                        {
                            AddMaterializedCaptures(pattern, binaries);
                        }
                    }
                    else
                    {
                        foreach (BitPattern pattern in clause.Patterns.OfType<BitPattern>())
                        {
                            AddCaptures(pattern, binaries);
                        }
                    }

                    return VisitClause(
                        clause,
                        [],
                        binaries,
                        caller,
                        clause
                    );
                }));

                active.Remove(key);

                return proved && calls != 0;

                bool VisitClause(
                    Clause clause,
                    HashSet<string> beforeMatch,
                    HashSet<string> afterMatch,
                    FunctionDefinition caller,
                    Clause? entry
                )
                {
                    int patternExpressions = clause.Guard is null ? 1 : 2;

                    var bodyFacts = afterMatch;
                    if ((materializedBitstringOnly || trackGuardedBinaryInputs) && clause.Guard is { } guarded)
                    {
                        bodyFacts = new HashSet<string>(afterMatch, StringComparer.Ordinal);
                        foreach (Expr.Call proof in PredicateConjuncts(guarded).OfType<Expr.Call>())
                        {
                            if (proof is
                                {
                                    DynamicRemote: false,
                                    Function: AutoImportNames.IsBinary or AutoImportNames.IsBitstring,
                                    Arguments: [Expr.Variable variable]
                                } && (proof.Function == AutoImportNames.IsBinary || materializedBitstringOnly)
                                    && owner.ResolvesErlangBif(proof))
                            {
                                bodyFacts.Add(variable.Name);
                            }
                        }
                    }

                    return LocalFunctionEntries.ClauseExpressions([clause]).SkipLast(patternExpressions)
                        .All(expression => Visit(
                            expression,
                            beforeMatch,
                            caller,
                            entry
                        ))
                        && (clause.Guard is null || Visit(
                            clause.Guard,
                            afterMatch,
                            caller,
                            entry
                        ))
                        && Visit(
                            clause.Body,
                            bodyFacts,
                            caller,
                            entry
                        );
                }

                bool Visit(
                    Expr expression,
                    HashSet<string> binaries,
                    FunctionDefinition caller,
                    Clause? entry
                )
                {
                    if (expression is Expr.FunctionReference reference && (reference.Name, reference.Arity) == signature)
                    {
                        return false;
                    }

                    if (expression is Expr.Call { Module: null, DynamicRemote: false } call
                        && (call.Function, call.Arguments.Count) == signature
                        && !ReferenceEquals(call, binaryRecursion))
                    {
                        calls++;
                        Expr value = call.Arguments[argument];
                        if (!IsBinaryValue(value, binaries)
                            && (materializedBitstringOnly || value is not Expr.Variable variable
                                || entry is null
                                || Array.FindIndex(entry.Patterns.ToArray(), pattern => pattern is Pattern.Variable binding
                                    && binding.Name == variable.Name) is not (>= 0 and var parameter)
                                || !Prove(caller, parameter, active)))
                        {
                            return false;
                        }
                    }

                    switch (expression)
                    {
                        case Expr.Sequence sequence:
                            var scope = new HashSet<string>(binaries, StringComparer.Ordinal);
                            foreach (Expr item in sequence.Items)
                            {
                                if (!Visit(
                                    item,
                                    scope,
                                    caller,
                                    entry
                                ))
                                {
                                    return false;
                                }

                                if (materializedBitstringOnly && item is Expr.Match materialized)
                                {
                                    AddMaterializedCaptures(materialized.Pattern, scope);
                                    if (materialized.Pattern is Pattern.Variable binding
                                        && IsMaterializedResult(materialized.Value, scope))
                                    {
                                        scope.Add(binding.Name);
                                    }
                                }
                                else if (item is Expr.Match { Pattern: BitPattern pattern })
                                {
                                    AddCaptures(pattern, scope);
                                }
                                else if (trackBinaryPairResults && item is Expr.Match
                                {
                                    Pattern: Pattern.Tuple
                                    {
                                        Items: [_, Pattern.Variable { Name: not VariableScopeNames.Wildcard } tail]
                                    },
                                    Value: Expr.Call { Module: null, DynamicRemote: false } pair
                                }
                                    && ReturnsBinaryPair(pair, []))
                                {
                                    scope.Add(tail.Name);
                                }
                                else if (projectedBinaryTuple is { } projection && item is Expr.Match match
                                    && ReadBinaryTupleProjection(match.Value, match.Pattern, projection) is { } projected)
                                {
                                    scope.Add(projected);
                                }
                            }

                            return true;
                        case Expr.Case decision:
                            return Visit(
                                decision.Value,
                                binaries,
                                caller,
                                entry
                            )
                                && decision.Clauses.All(branch =>
                                {
                                    var branchScope = new HashSet<string>(binaries, StringComparer.Ordinal);
                                    if (materializedBitstringOnly)
                                    {
                                        foreach (Pattern pattern in branch.Patterns)
                                        {
                                            AddMaterializedCaptures(pattern, branchScope);
                                        }
                                    }
                                    else if (branch.Patterns is [BitPattern pattern])
                                    {
                                        AddCaptures(pattern, branchScope);
                                    }
                                    else if (projectedBinaryTuple is { } projection && branch.Patterns is [var tuple]
                                        && ReadBinaryTupleProjection(decision.Value, tuple, projection) is { } projected)
                                    {
                                        branchScope.Add(projected);
                                    }

                                    return VisitClause(
                                        branch,
                                        binaries,
                                        branchScope,
                                        caller,
                                        entry
                                    );
                                });
                        case Expr.ListComprehension list when list.Qualifiers.FirstOrDefault() is ComprehensionQualifier.Generator generator:
                            return Visit(
                                generator.Source,
                                binaries,
                                caller,
                                entry
                            )
                                && LocalFunctionEntries.QualifierExpressions([generator with { Source = new Expr.Literal(Nil.Value) }])
                                    .Concat(LocalFunctionEntries.QualifierExpressions(list.Qualifiers.Skip(1)))
                                    .Concat(list.Items).All(item => Visit(
                                        item,
                                        [],
                                        caller,
                                        null
                                    ));
                        case Expr.Try attempted when trackGuardedBinaryInputs:
                            // The try body starts in the caller's scope. Bindings in
                            // of/catch/after do not participate in the entry proof.
                            return Visit(
                                attempted.Body,
                                binaries,
                                caller,
                                entry
                            )
                                && LocalFunctionEntries.Children(attempted).Where(item => !ReferenceEquals(item, attempted.Body))
                                    .All(item => Visit(
                                        item,
                                        [],
                                        caller,
                                        null
                                    ));
                        case Expr.Fun or Expr.ListComprehension or Expr.BinaryComprehension or Expr.MapComprehension
                            or Expr.Try or Expr.Receive or Expr.Maybe:
                            return LocalFunctionEntries.Children(expression).All(item => Visit(
                                item,
                                [],
                                caller,
                                null
                            ));
                        default:
                            return LocalFunctionEntries.Children(expression).All(item => Visit(
                                item,
                                binaries,
                                caller,
                                entry
                            ));
                    }
                }
            }

            bool IsBinaryValue(Expr value, HashSet<string> binaries) =>
                materializedBitstringOnly ? IsMaterializedResult(value, binaries)
                    : value is Expr.Variable variable && binaries.Contains(variable.Name)
                    || value is Expr.Literal { Value: BitString { IsBinary: true } }
                    || value is Expr.Bits bits && bits.Segments.All(segment => segment.Unit % BitSyntaxDefaults.BinaryUnit == 0
                        || FixedSize(segment) is int size && size * segment.Unit % BitSyntaxDefaults.BinaryUnit == 0);

            bool IsMaterializedResult(Expr value, HashSet<string> raw)
            {
                if (value is Expr.Bits or Expr.Literal { Value: BitString }
                    || value is Expr.Variable variable && raw.Contains(variable.Name))
                {
                    return true;
                }

                if (value is not Expr.Case decision)
                {
                    return false;
                }

                bool hasResult = false;
                foreach (Clause branch in decision.Clauses)
                {
                    if (branch.Body is Expr.Call
                        {
                            Function: AutoImportNames.Throw, Arguments.Count: 1, DynamicRemote: false
                        } terminal && owner.ResolvesErlangBif(terminal))
                    {
                        continue;
                    }

                    var captures = new HashSet<string>(raw, StringComparer.Ordinal);
                    foreach (Pattern pattern in branch.Patterns)
                    {
                        AddMaterializedCaptures(pattern, captures);
                    }

                    if (!IsMaterializedResult(branch.Body, captures))
                    {
                        return false;
                    }

                    hasResult = true;
                }

                return hasResult;
            }

            static void AddMaterializedCaptures(Pattern pattern, HashSet<string> raw)
            {
                switch (pattern)
                {
                    case AliasPattern alias:
                        AddMaterializedCaptures(alias.Left, raw);
                        AddMaterializedCaptures(alias.Right, raw);
                        break;
                    case Pattern.Tuple tuple:
                        foreach (Pattern member in tuple.Items)
                        {
                            AddMaterializedCaptures(member, raw);
                        }

                        break;
                    case BitPattern bits:
                        foreach (BitPatternSegment segment in bits.Segments)
                        {
                            // An open suffix can be carried as a match context.
                            // A finite field is an extracted, materialized value.
                            if (segment is
                                {
                                    Value: Pattern.Variable { Name: not VariableScopeNames.Wildcard } capture,
                                    Specification: { Type: BitSegmentTypes.Binary, Size: not null } field
                                } && FixedSize(field) is not null)
                            {
                                raw.Add(capture.Name);
                            }
                        }

                        break;
                }
            }

            static void AddCaptures(BitPattern pattern, HashSet<string> binaries)
            {
                foreach (BitPatternSegment segment in pattern.Segments)
                {
                    if (segment.Specification is { Type: BitSegmentTypes.Binary, Unit: BitSyntaxDefaults.BinaryUnit }
                        && segment.Value is Pattern.Variable { Name: not VariableScopeNames.Wildcard } capture)
                    {
                        binaries.Add(capture.Name);
                    }
                }
            }

            bool ReturnsBinaryPair(Expr.Call call, HashSet<(string Name, int Arity)> active)
            {
                var signature = (call.Function, call.Arguments.Count);
                if (owner.IsDeclaredNative(signature) || !localFunctions.TryGetValue(signature, out var target))
                {
                    return false;
                }

                // Recursive calls only count when they are the final expression of a checked clause.
                if (!active.Add(signature))
                {
                    return true;
                }

                bool known = target.Clauses.All(clause =>
                {
                    var captures = new HashSet<string>(StringComparer.Ordinal);
                    foreach (var pattern in clause.Patterns.OfType<BitPattern>())
                    {
                        AddCaptures(pattern, captures);
                    }

                    Expr result = clause.Body;
                    if (result is Expr.Sequence { Items: [.., var last] })
                    {
                        result = last;
                    }

                    return result switch
                    {
                        Expr.Tuple { Items: [_, var side] } => IsBinaryValue(side, captures),
                        Expr.Call { Module: null, DynamicRemote: false } forwarded => ReturnsBinaryPair(forwarded, active),
                        _ => false
                    };
                });
                active.Remove(signature);

                return known;
            }
        }
    }
}
