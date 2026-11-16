namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool HasBinaryLocalArgument(FunctionDefinition function, Expr.Call binaryRecursion)
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
                    foreach (BitPattern pattern in clause.Patterns.OfType<BitPattern>())
                    {
                        AddCaptures(pattern, binaries);
                    }

                    return VisitClause(clause, [], binaries, caller, clause);
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
                    return LocalFunctionEntries.ClauseExpressions([clause]).SkipLast(patternExpressions)
                        .All(expression => Visit(expression, beforeMatch, caller, entry))
                        && (clause.Guard is null || Visit(clause.Guard, afterMatch, caller, entry))
                        && Visit(clause.Body, afterMatch, caller, entry);
                }

                bool Visit(Expr expression, HashSet<string> binaries, FunctionDefinition caller, Clause? entry)
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
                            && (value is not Expr.Variable variable
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
                                if (!Visit(item, scope, caller, entry))
                                {
                                    return false;
                                }

                                if (item is Expr.Match { Pattern: BitPattern pattern })
                                {
                                    AddCaptures(pattern, scope);
                                }
                            }

                            return true;
                        case Expr.Case decision:
                            return Visit(decision.Value, binaries, caller, entry)
                                && decision.Clauses.All(branch =>
                                {
                                    var branchScope = new HashSet<string>(binaries, StringComparer.Ordinal);
                                    if (branch.Patterns is [BitPattern pattern])
                                    {
                                        AddCaptures(pattern, branchScope);
                                    }

                                    return VisitClause(branch, binaries, branchScope, caller, entry);
                                });
                        case Expr.ListComprehension list when list.Qualifiers.FirstOrDefault() is ComprehensionQualifier.Generator generator:
                            return Visit(generator.Source, binaries, caller, entry)
                                && LocalFunctionEntries.QualifierExpressions([generator with { Source = new Expr.Literal(Nil.Value) }])
                                    .Concat(LocalFunctionEntries.QualifierExpressions(list.Qualifiers.Skip(1)))
                                    .Concat(list.Items).All(item => Visit(item, [], caller, null));
                        case Expr.Fun or Expr.ListComprehension or Expr.BinaryComprehension or Expr.MapComprehension
                            or Expr.Try or Expr.Receive or Expr.Maybe:
                            return LocalFunctionEntries.Children(expression).All(item => Visit(item, [], caller, null));
                        default:
                            return LocalFunctionEntries.Children(expression).All(item => Visit(item, binaries, caller, entry));
                    }
                }
            }

            static bool IsBinaryValue(Expr value, HashSet<string> binaries) =>
                value is Expr.Variable variable && binaries.Contains(variable.Name)
                || value is Expr.Literal { Value: BitString { IsBinary: true } }
                || value is Expr.Bits bits && bits.Segments.All(segment => segment.Unit % BitSyntaxDefaults.BinaryUnit == 0
                    || FixedSize(segment) is int size && size * segment.Unit % BitSyntaxDefaults.BinaryUnit == 0);

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
        }
    }
}
