namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private FunctionDefinition FoldBinaryMatchTupleBindings(FunctionDefinition function)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled)
        {
            return function;
        }

        return function with { Clauses = function.Clauses.Select(FoldClause).ToArray() };

        static Clause FoldClause(Clause clause)
        {
            if (clause.Body is not Expr.Sequence sequence
                || !sequence.Items.SkipLast(1).Any(item => item is Expr.Match { Pattern: Pattern.Tuple, Value: Expr.Block }))
            {
                return clause;
            }

            Expr[] expressions = LocalFunctionEntries.ClauseExpressions([clause])
                .SelectMany(LocalFunctionEntries.Walk).ToArray();
            var bindings = clause.Patterns.SelectMany(Semantics.Variables)
                .Concat(expressions.SelectMany(NestedPatterns).SelectMany(Semantics.Variables))
                .Concat(expressions.OfType<Expr.Fun>().Select(fun => fun.SelfName).OfType<string>())
                .GroupBy(name => name, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
            var reads = expressions.OfType<Expr.Variable>().Select(variable => variable.Name)
                .GroupBy(name => name, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
            Expr[]? folded = null;
            for (int index = 0; index < sequence.Items.Count - 1; index++)
            {
                if (sequence.Items[index] is not Expr.Match
                    {
                        Pattern: Pattern.Tuple { Items: [Pattern.Variable first, Pattern.Variable second] },
                        Value: Expr.Block
                        {
                            Body: Expr.Sequence
                            {
                                Items:
                                [
                                    Expr.Match { Pattern: BitPattern { Segments: [var firstSegment, var secondSegment] } bits } inner,
                                    Expr.Tuple { Items: [Expr.Variable firstRead, Expr.Variable secondRead] }
                                ]
                            }
                        }
                    }
                    || firstSegment.Value is not Pattern.Variable firstInner
                    || secondSegment.Value is not Pattern.Variable secondInner
                    || firstRead.Name != firstInner.Name || secondRead.Name != secondInner.Name
                    || !Fresh(first.Name) || !Fresh(second.Name) || !Fresh(firstInner.Name) || !Fresh(secondInner.Name)
                    || reads.GetValueOrDefault(firstInner.Name) != 1 || reads.GetValueOrDefault(secondInner.Name) != 1)
                {
                    continue;
                }

                // Core tuple expansion binds the components individually. Keep the inner match's
                // subject, checks and source location, including its original badmatch path.
                folded ??= sequence.Items.ToArray();
                folded[index] = inner with
                {
                    Pattern = bits with
                    {
                        Segments = [Rename(firstSegment, first), Rename(secondSegment, second)]
                    }
                };
            }

            return folded is null ? clause : clause with { Body = sequence with { Items = folded } };

            bool Fresh(string name) => name != VariableScopeNames.Wildcard && bindings.GetValueOrDefault(name) == 1;
        }
    }

    private static BitPatternSegment Rename(BitPatternSegment segment, Pattern.Variable variable) =>
        segment with
        {
            Value = variable,
            Specification = segment.Specification with { Value = new Expr.Variable(variable.Name) }
        };

    private static IEnumerable<Pattern> NestedPatterns(Expr expression) => expression switch
    {
        Expr.Match match => [match.Pattern],
        Expr.MaybeMatch match => [match.Pattern],
        Expr.Maybe maybe => maybe.Clauses.SelectMany(clause => clause.Patterns),
        Expr.Case choice => choice.Clauses.SelectMany(clause => clause.Patterns),
        Expr.If choice => choice.Clauses.SelectMany(clause => clause.Patterns),
        Expr.Receive receive => receive.Clauses.SelectMany(clause => clause.Patterns),
        Expr.Try attempt => attempt.Clauses.Concat(attempt.Catches).SelectMany(clause => clause.Patterns),
        Expr.Fun fun => fun.Clauses.SelectMany(clause => clause.Patterns),
        Expr.ListComprehension list => list.Qualifiers.SelectMany(GeneratorPatterns),
        Expr.BinaryComprehension binary => binary.Qualifiers.SelectMany(GeneratorPatterns),
        Expr.MapComprehension map => map.Qualifiers.SelectMany(GeneratorPatterns),
        _ => []
    };

    private static IEnumerable<Pattern> GeneratorPatterns(ComprehensionQualifier qualifier) => qualifier switch
    {
        ComprehensionQualifier.Generator generator => [generator.Pattern],
        ComprehensionQualifier.BinaryGenerator generator => [generator.Pattern],
        ComprehensionQualifier.MapGenerator generator => [generator.Pattern],
        ComprehensionQualifier.Zip zip => zip.Generators.SelectMany(GeneratorPatterns),
        _ => []
    };
}
