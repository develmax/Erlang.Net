using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed record ListMapperCall(Expr.Call Call, Term?[] Arguments);

    private sealed record ClosedListMapper(
        Expr.Variable Source,
        ComprehensionQualifier.Generator Generator,
        IReadOnlyList<ListMapperCall> Calls,
        IReadOnlySet<string> Locals,
        ListMapperFilter? Filter
    );

    private static ClosedListMapper? ReadClosedListMapper(Expr expression)
    {
        if (expression is not Expr.ListComprehension
            {
                Items: [var mapped],
                Qualifiers: [ComprehensionQualifier.Generator
                {
                    Strict: false, Pattern: Pattern.Variable item, Source: Expr.Variable source
                } generator, ..] qualifiers
            }
            || item.Name == VariableScopeNames.Wildcard)
        {
            return null;
        }

        ListMapperFilter? filter = null;
        if (qualifiers is [_, ComprehensionQualifier.Filter condition])
        {
            bool negate = condition.Expression is Expr.Unary { Operator: ErlangOperators.Not };
            Expr test = negate ? ((Expr.Unary)condition.Expression).Operand : condition.Expression;
            var predicate = new List<ListMapperCall>();
            if (!Append(test, item.Name, predicate))
            {
                return null;
            }

            filter = new(condition.Expression, predicate, negate);
        }
        else if (qualifiers.Count != 1)
        {
            return null;
        }

        var calls = new List<ListMapperCall>();
        var locals = new HashSet<string>(StringComparer.Ordinal);
        string input = item.Name;
        if (mapped is Expr.Block block)
        {
            mapped = block.Body;
        }

        if (mapped is Expr.Sequence { Items: [.., var result] } sequence)
        {
            foreach (Expr stage in sequence.Items.SkipLast(1))
            {
                if (stage is not Expr.Match { Pattern: Pattern.Variable local, Value: var value }
                    || local.Name == VariableScopeNames.Wildcard || local.Name == item.Name
                    || !locals.Add(local.Name) || !Append(value, input, calls))
                {
                    return null;
                }

                input = local.Name;
            }

            mapped = result;
        }

        bool returnsHead = filter is not null && locals.Count == 0
            && mapped is Expr.Variable output && output.Name == item.Name;

        return returnsHead || Append(mapped, input, calls) ? new(
            source,
            generator,
            calls,
            locals,
            filter
        ) : null;

        static bool Append(Expr value, string variable, List<ListMapperCall> stages)
        {
            if (value is not Expr.Call { DynamicRemote: false } call
                || call.Arguments.Count is 0 or >= FirstReservedXRegister)
            {
                return false;
            }

            var arguments = new Term?[call.Arguments.Count];
            Expr? nested = null;
            int inputs = 0;
            for (int index = 0; index < call.Arguments.Count; index++)
            {
                Expr argument = call.Arguments[index];
                if (AttributeLiteral.TryNormalizeConstant(argument, out Term constant))
                {
                    arguments[index] = constant;
                    continue;
                }

                inputs++;
                if (argument is not Expr.Variable reference || reference.Name != variable)
                {
                    nested = argument;
                }
            }

            if (inputs == 0 || nested is not null && (inputs != 1 || !Append(nested, variable, stages)))
            {
                return false;
            }

            stages.Add(new(call, arguments));

            return true;
        }
    }

    private bool CanExtractClosedListMapper(
        Expr comprehension,
        ClosedListMapper mapper,
        Clause clause,
        HashSet<(string, int)> declared
    )
    {
        if (ReadListMappingPlan([clause]) is not null
            || mapper.Filter is { } filter && ComprehensionGuard.IsTest(filter.Expression, sourceModule)
            || mapper.Calls.Concat(mapper.Filter?.Calls ?? []).Any(stage => stage.Call.Module is null
                && !declared.Contains((stage.Call.Function, stage.Arguments.Length))
                && ResolveExternalModule(stage.Call.Function, stage.Arguments.Length) is null))
        {
            return false;
        }

        if (mapper.Locals.Count == 0)
        {
            return true;
        }

        // A match in the mapper may compare an existing parent binding.
        // Only move the block when all its assignment targets are fresh.
        return !clause.Patterns.SelectMany(Semantics.Variables).Any(mapper.Locals.Contains)
            && !OuterBindings(clause.Body).Any(mapper.Locals.Contains);

        IEnumerable<string> OuterBindings(Expr expression)
        {
            if (ReferenceEquals(expression, comprehension))
            {
                yield break;
            }

            IEnumerable<Pattern> patterns = expression switch
            {
                Expr.Match match => [match.Pattern],
                Expr.MaybeMatch match => [match.Pattern],
                Expr.Case decision => decision.Clauses.SelectMany(branch => branch.Patterns),
                Expr.Try attempt => attempt.Clauses.Concat(attempt.Catches).SelectMany(branch => branch.Patterns),
                Expr.If decision => decision.Clauses.SelectMany(branch => branch.Patterns),
                Expr.Maybe attempt => attempt.Clauses.SelectMany(branch => branch.Patterns),
                _ => []
            };
            foreach (string name in patterns.SelectMany(Semantics.Variables))
            {
                yield return name;
            }

            foreach (string name in LocalFunctionEntries.Children(expression).SelectMany(OuterBindings))
            {
                yield return name;
            }
        }
    }

    private sealed partial class GeneralEmitter
    {
        private bool TryEmitClosedListMapper(FunctionDefinition function)
        {
            if (!function.IsClosedListMapper || function.Clauses is not [var clause]
                || ReadClosedListMapper(clause.Body) is not { } mapper)
            {
                return false;
            }

            if (mapper.Filter is { } filter)
            {
                EmitClosedListFilter(mapper, filter);

                return true;
            }

            int entry = owner.nextLabel - 1;
            int empty = Label();
            int invalid = Label();
            owner.EmitNonemptyList(0, empty);
            EmitFrameAllocation(1, 1);
            owner.EmitListParts(
                0,
                XRegisterTag,
                0,
                YRegisterTag,
                0
            );

            var current = EmitListMapperCalls(mapper.Calls, location);
            owner.EmitMappingResult(entry, current == location ? null : location);
            owner.EmitMappingEnd(empty, invalid);

            return true;
        }

        private BeamSourceLocation EmitListMapperCalls(
            IReadOnlyList<ListMapperCall> calls,
            BeamSourceLocation current,
            bool loadFilterHead = false
        )
        {
            foreach (ListMapperCall stage in calls)
            {
                if (loadFilterHead)
                {
                    owner.EmitOrderedTailMoves(
                        [],
                        stage.Arguments.Select((literal, index) => (Destination: index, Value: literal!)).Skip(1).ToList(),
                        [(0, 1)]
                    );
                }
                else
                {
                    for (int index = stage.Arguments.Length - 1; index >= 0; index--)
                    {
                        if (stage.Arguments[index] is { } literal)
                        {
                            owner.EmitConstant(literal, index);
                        }
                        else if (index != 0)
                        {
                            owner.MoveRegister(
                                XRegisterTag,
                                0,
                                XRegisterTag,
                                index
                            );
                        }
                    }
                }

                Expr.Call call = stage.Call;
                owner.EmitCallLine(call, current);
                if (call.Module is null && entries.TryGetValue((call.Function, stage.Arguments.Length), out int local))
                {
                    owner.EmitFieldCall(stage.Arguments.Length, local);
                }
                else
                {
                    string module = call.Module ?? owner.ResolveExternalModule(call.Function, stage.Arguments.Length)
                        ?? throw new NotSupportedException(Unsupported);
                    owner.Op(ExternalCall, BeamWire.UnsignedTag, stage.Arguments.Length);
                    owner.Operand(BeamWire.UnsignedTag, owner.Import(module, call.Function, stage.Arguments.Length));
                }

                current = CallLocation(call, current);
            }

            return current;
        }
    }
}
